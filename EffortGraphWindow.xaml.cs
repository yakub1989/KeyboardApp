using OxyPlot;
using OxyPlot.Series;
using System.Windows;

namespace KeyboardApp
{
    public partial class EffortGraphWindow : Window
    {
        public EffortGraphWindow(double[] generationEffort)
        {
            InitializeComponent();

            // Create the plot model
            var plotModel = new PlotModel { Title = "Effort Over Generations" };

            // Create a line series to represent effort over generations
            var effortSeries = new LineSeries
            {
                Title = "Effort",
                MarkerType = MarkerType.None,
                MarkerSize = 4,
                LineStyle = LineStyle.Solid
            };

            // Add data points to the series (generation index vs effort value)
            for (int i = 0; i < generationEffort.Length; i++)
            {
                effortSeries.Points.Add(new DataPoint(i + 1, generationEffort[i])); // i + 1 to match the generation numbering
            }

            // Add the series to the plot model
            plotModel.Series.Add(effortSeries);

            // Set the model for the PlotView control
            EffortPlotView.Model = plotModel;
        }
    }
}
