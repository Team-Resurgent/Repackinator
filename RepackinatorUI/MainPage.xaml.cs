
using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace RepackinatorUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();


            var grid = new Grid
            {
                ColumnDefinitions = Columns.Define(150, 300, 150, 200, 300, 300, 100),
                RowDefinitions = Rows.Define(Auto, Star),

                Children =
                {
                    new Label().Text("Title ID").Row(0).Column(0),
                    new Label().Text("Title Name").Row(0).Column(1),
                    new Label().Text("Version").Row(0).Column(2),
                    new Label().Text("Region").Row(0).Column(3),
                    new Label().Text("XBE Title & Folder Name").Row(0).Column(4),
                    new Label().Text("ISO Name").Row(0).Column(5),
                    new Label().Text("Process").Row(0).Column(6)
                }
            };
            grid.FillVertical();
            GridContent.Content = grid;
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}