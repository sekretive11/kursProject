using CsvHelper;
using KursProject.Data;
using KursProject.Models;
using KursProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot;
using System.Globalization;

namespace KursProject.Pages
{
    public class IndexModel : PageModel
    {
        private readonly kursProjContext _context;
        private readonly CsvDataService _csvDataService;

        public IndexModel(kursProjContext context, CsvDataService csvDataService)
        {
            _context = context;
            _csvDataService = csvDataService;
        }

        public List<marketingdata> DataPoints { get; set; } = new();
        public List<(string RangeLabel, double AverageSales)> HistogramData { get; set; } = new();

        public double? Correlation { get; set; }
        public (double Slope, double Intercept) Regression { get; set; }

        [BindProperty]
        public FormulaSelection FormulaSelection { get; set; } = new FormulaSelection
        {
            UseMntWines = true,
            UseMntFruits = true,
            UseMntMeatProducts = true,
            UseMntFishProducts = true,
            UseMntSweetProducts = true,
            UseMntGoldProds = true,
            UseNumDealsPurchases = true,
            UseNumWebPurchases = true,
            UseNumCatalogPurchases = true,
            UseNumStorePurchases = true
        };

        public async Task OnGetAsync()
        {
            if (!_context.marketingdata.Any())
            {
                var csvData = _csvDataService.Loadmarketingdata("Datasets/ifood_df.csv", new FormulaSelection
                {
                    UseMntWines = true,
                    UseMntFruits = true,
                    UseMntMeatProducts = true,
                    UseMntFishProducts = true,
                    UseMntSweetProducts = true,
                    UseMntGoldProds = true,
                    UseNumDealsPurchases = true,
                    UseNumWebPurchases = true,
                    UseNumCatalogPurchases = true,
                    UseNumStorePurchases = true
                });

                await _context.marketingdata.AddRangeAsync(csvData);
                await _context.SaveChangesAsync();
            }

            DataPoints = await _context.marketingdata.ToListAsync();
            CalculateStats();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            _context.marketingdata.RemoveRange(_context.marketingdata);
            await _context.SaveChangesAsync();

            var csvData = _csvDataService.Loadmarketingdata("Datasets/ifood_df.csv", FormulaSelection);

            await _context.marketingdata.AddRangeAsync(csvData);
            await _context.SaveChangesAsync();

            DataPoints = await _context.marketingdata.ToListAsync();
            CalculateStats();

            return Page();
        }

        public async Task<IActionResult> OnPostGeneratePdfAsync()
        {
            DataPoints = await _context.marketingdata.ToListAsync();
            CalculateStats();
            var pdfBytes = await GeneratePdfReportAsync(FormulaSelection);
            return File(pdfBytes, "application/pdf", "MarketingReport.pdf");
        }

        private void CalculateStats()
        {
            if (DataPoints.Count < 2)
            {
                Correlation = null;
                return;
            }

            var adCosts = DataPoints.Select(d => (double)d.AdCost).ToArray();
            var sales = DataPoints.Select(d => (double)d.SalesVolume).ToArray();

            Correlation = CalculatePearsonCorrelation(adCosts, sales);


            if (adCosts.Distinct().Count() == 1)
            {
                Regression = (0, 0);
            }
            else
            {
                Regression = CalculateLinearRegression(adCosts, sales);
            }

            var grouped = DataPoints
                .GroupBy(d => ((int)d.AdCost / 500) * 500)
                .OrderBy(g => g.Key)
                .Select(g => (
                    RangeLabel: $"{g.Key}-{g.Key + 500}",
                    AverageSales: g.Average(x => (double)x.SalesVolume)
                ))
                .ToList();

            HistogramData = grouped;
        }

        private double CalculatePearsonCorrelation(double[] x, double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("������� ������ ����� ���������� �����");

            if (x.Length == 0)
                return 0;

            double meanX = x.Average();
            double meanY = y.Average();

            double numerator = 0;
            double sumSqX = 0;
            double sumSqY = 0;

            for (int i = 0; i < x.Length; i++)
            {
                double devX = x[i] - meanX;
                double devY = y[i] - meanY;

                numerator += devX * devY;
                sumSqX += devX * devX;
                sumSqY += devY * devY;
            }

            double denominator = Math.Sqrt(sumSqX * sumSqY);
            return denominator == 0 ? 0 : numerator / denominator;
        }

        private (double Slope, double Intercept) CalculateLinearRegression(double[] x, double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("������� ������ ����� ���������� �����");

            double meanX = x.Average();
            double meanY = y.Average();

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < x.Length; i++)
            {
                numerator += (x[i] - meanX) * (y[i] - meanY);
                denominator += Math.Pow(x[i] - meanX, 2);
            }

            if (denominator == 0)
                throw new InvalidOperationException("���������� ���������� ���������: ��� �������� X ���������");

            double slope = numerator / denominator;
            double intercept = meanY - slope * meanX;

            return (slope, intercept);
        }


        private async Task<byte[]> GeneratePdfReportAsync(FormulaSelection currentSelection)
        {
            Console.WriteLine($"UseMntWines: {currentSelection.UseMntWines}");

            QuestPDF.Settings.License = LicenseType.Community;

            var scatterChartTask = GenerateChartImage();
            var regressionChartTask = Correlation.HasValue ? GenerateRegressionChartImage() : Task.FromResult<byte[]?>(null);
            var histogramChartTask = GenerateHistogramChartImage();

            await Task.WhenAll(scatterChartTask, regressionChartTask);

            var scatterChartImage = await scatterChartTask;
            var regressionChartImage = await regressionChartTask;
            var histogramChartImage = await histogramChartTask;

            var selectedAdCosts = new List<string>();
            if (currentSelection.UseMntWines) selectedAdCosts.Add("�������");
            if (currentSelection.UseMntFruits) selectedAdCosts.Add("������");
            if (currentSelection.UseMntMeatProducts) selectedAdCosts.Add("������ ���������");
            if (currentSelection.UseMntFishProducts) selectedAdCosts.Add("����");
            if (currentSelection.UseMntSweetProducts) selectedAdCosts.Add("��������");
            if (currentSelection.UseMntGoldProds) selectedAdCosts.Add("��������� �������");

            var selectedSalesVolumes = new List<string>();
            if (currentSelection.UseNumDealsPurchases) selectedSalesVolumes.Add("������� �� ��������");
            if (currentSelection.UseNumWebPurchases) selectedSalesVolumes.Add("������� ����� ���-�������");
            if (currentSelection.UseNumCatalogPurchases) selectedSalesVolumes.Add("������� ����� ��������");
            if (currentSelection.UseNumStorePurchases) selectedSalesVolumes.Add("������� � ���������");

            Console.WriteLine($"��������� �������: {string.Join(", ", selectedAdCosts)}");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header().Element(container =>
                    {
                        container
                            .AlignCenter()
                            .PaddingBottom(15)
                            .Text(text =>
                            {
                                text.Span("����� �� �������������� �������")
                                    .Bold()
                                    .FontSize(20)
                                    .FontColor(Colors.Blue.Medium);
                            });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(20);

                        col.Item().Text(text => {
                            text.Span("��������� ����������:").Bold();
                        });

                        col.Item().Text(text => {
                            text.Span("��� ������ �� �������: ").SemiBold();
                            text.Span(string.Join(", ", selectedAdCosts));
                        });

                        col.Item().Text(text => {
                            text.Span("��� ������� ������: ").SemiBold();
                            text.Span(string.Join(", ", selectedSalesVolumes));
                        });

                        col.Item().Text("1. ������ ����������� ������ ������ �� ��������� ������")
                            .Bold().FontSize(14).FontColor(Colors.Black);

                        col.Item().Image(scatterChartImage).FitWidth();

                        if (regressionChartImage != null)
                        {
                            col.Item().Text("2. ������������� ������").Bold().FontSize(14).FontColor(Colors.Black);
                            col.Item().Image(regressionChartImage).FitWidth();
                        }

                        col.Item().Text("3. ����������� ������� ������ �� ���������� ������")
                            .Bold().FontSize(14).FontColor(Colors.Black);
                        col.Item().Image(histogramChartImage).FitWidth();

                        if (Correlation.HasValue)
                        {
                            col.Item().Text("4. ���������� �������").Bold().FontSize(14).FontColor(Colors.Black);
                            col.Item().Text(text =>
                            {
                                text.Span("����������� ����������: ").SemiBold();
                                text.Span(Correlation.Value.ToString("0.00"));
                            });
                            col.Item().Text(text =>
                            {
                                text.Span("��������� �������� ���������: ").SemiBold();
                                text.Span($"y = {Regression.Slope:0.00}x + {Regression.Intercept:0.00}");
                            });
                        }


                        col.Item().Element(container =>
                        {
                            container
                                .DefaultTextStyle(x => x.FontSize(14).FontColor(Colors.Black))
                                .Text(text =>
                                {
                                    text.Span("\n\n�����: ").Bold();
                                    text.Span("� ���� �������������-�������������� ������� �������� ������� �������� ������ �� ������� � ������ ������ �� �������� ������������ ���������� �� �������:\n\n");
                                    text.Span(" 1. � ������ ��������� ������ �������� ������������ ���������� �����������, ��� ��������������� � ����� �������� �����������\n\n");
                                    text.Span(" 2. � ������ ������ ������ �������� ������������ ���������� �������������, ��� ��������������� � ����� ������ �����������");
                                });
                        });

                    });

                    page.Footer().Element(footer =>
                    {
                        footer.AlignCenter()
                              .Text(text =>
                              {
                                  text.Span("������������� ������������� � ")
                                      .FontSize(10)
                                      .FontColor(Colors.Grey.Medium);

                                  text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}")
                                      .FontSize(10)
                                      .FontColor(Colors.Grey.Medium);
                              });
                    });






                });
            });

            return document.GeneratePdf();
        }


        private Task<byte[]> GenerateChartImage()
        {
            var plt = new ScottPlot.Plot(600, 400);
            var xs = DataPoints.Select(p => (double)p.AdCost).ToArray();
            var ys = DataPoints.Select(p => (double)p.SalesVolume).ToArray();

            plt.AddScatterPoints(xs, ys, color: System.Drawing.Color.Teal, markerSize: 4);
            plt.Title("������� vs �������");
            plt.XLabel("������� �� �������");
            plt.YLabel("����� ������");

            return Task.FromResult(plt.GetImageBytes());
        }

        private Task<byte[]> GenerateRegressionChartImage()
        {
            var plt = new ScottPlot.Plot(600, 400);
            var xs = DataPoints.Select(p => (double)p.AdCost).ToArray();
            var ys = DataPoints.Select(p => (double)p.SalesVolume).ToArray();

            plt.AddScatterPoints(xs, ys, color: System.Drawing.Color.Teal, markerSize: 4, label: "������");

            double minX = xs.Min();
            double maxX = xs.Max();
            double[] lineXs = { minX, maxX };
            double[] lineYs = { Regression.Slope * minX + Regression.Intercept, Regression.Slope * maxX + Regression.Intercept };

            plt.AddScatter(lineXs, lineYs, color: System.Drawing.Color.Red, lineWidth: 2, label: "���������");
            plt.Legend();
            plt.Title("������������� ������");
            plt.XLabel("������� �� �������");
            plt.YLabel("����� ������");

            return Task.FromResult(plt.GetImageBytes());
        }

        private Task<byte[]> GenerateHistogramChartImage()
        {
            var plt = new ScottPlot.Plot(600, 400);

            var labels = HistogramData.Select(h => h.RangeLabel).ToArray();
            var values = HistogramData.Select(h => h.AverageSales).ToArray();

            var bar = plt.AddBar(values);
            bar.FillColor = System.Drawing.Color.CornflowerBlue;

            plt.XTicks(Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);
            plt.XLabel("��������� ������ �� �������");
            plt.YLabel("������� ����� ������");
            plt.Title("�����������: ������� ������� �� ���������� ������");

            plt.SetAxisLimits(yMin: 0);
            plt.Layout(bottom: 60);

            return Task.FromResult(plt.GetImageBytes());
        }

    }
}