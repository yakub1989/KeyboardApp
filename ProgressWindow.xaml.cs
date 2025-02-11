using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KeyboardApp
{
    public partial class ProgressWindow : Window
    {
        private readonly string[] animationCycle = { " | ", " / ", " — ", " \\ " };
        private int animationIndex = 0;
        private CancellationTokenSource animationCancellationToken;

        public ProgressWindow()
        {
            InitializeComponent();
            StartAnimationAsync();
        }

        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() => StatusLabel.Text = message);
        }

        private async void StartAnimationAsync()
        {
            animationCancellationToken = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                try
                {
                    while (!animationCancellationToken.Token.IsCancellationRequested)
                    {
                        await Task.Delay(500, animationCancellationToken.Token);

                        Dispatcher.Invoke(() =>
                        {
                            AnimationLabel.Text = animationCycle[animationIndex];
                            animationIndex = (animationIndex + 1) % animationCycle.Length;
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Task został anulowany - zamknięcie okna
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            animationCancellationToken?.Cancel();
            animationCancellationToken?.Dispose();
            base.OnClosed(e);
        }
    }
}
