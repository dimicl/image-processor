using Microsoft.Win32;
using Shared.DTOs;
using Shared.MSI;
using Shared.Utils;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Client;

public partial class MainWindow : Window
{
    private const int MaxHistory = 3;

    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5265/")
    };

    private BitmapImage? _current;
    private BitmapImage? _original;
    private Guid? _sessionId;

    private readonly List<BitmapImage> _undo = new();
    private readonly List<BitmapImage> _redo = new();

    private double _zoom = 1;
    private System.Windows.Point _panStart;
    private double _panStartX, _panStartY;
    private bool _dragging;

    public MainWindow()
    {
        InitializeComponent();
        CmbQueueFilter.SelectedIndex = 0;
    }

    #region Load / Export

    private async void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Slike|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.msi|MSI|*.msi|Sve|*.*"
        };

        if (ofd.ShowDialog() != true) return;

        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(ofd.FileName);
            var ext = Path.GetExtension(ofd.FileName).ToLowerInvariant();

            BitmapImage img;
            if (ext == ".msi")
            {
                using var bmp = MsiDecoder.Decode(bytes);
                img = BitmapToBitmapImage(bmp);
            }
            else
            {
                img = BytesToBitmapImage(bytes);
            }

            _original = CloneBitmap(img);
            SetImage(img, resetHistory: true);

            var uploadReq = new UploadImageRequest
            {
                Image = ImageToPngBytes(img),
                FileName = Path.GetFileName(ofd.FileName)
            };
            var uploadJson = JsonSerializer.Serialize(uploadReq);
            var uploadResp = await Http.PostAsync("api/image/upload",
                new StringContent(uploadJson, Encoding.UTF8, "application/json"));

            if (uploadResp.IsSuccessStatusCode)
            {
                var resp = JsonSerializer.Deserialize<UploadImageResponse>(
                    await uploadResp.Content.ReadAsStringAsync(),
                    JsonOptions());
                _sessionId = resp?.ImageId;
                ClientLogger.Info($"Upload OK id={_sessionId} file={ofd.FileName} size={bytes.Length}B");
                TxtStatus.Text = $"Učitano: {Path.GetFileName(ofd.FileName)} ({bytes.Length / 1024} KB)";
            }

            BtnExportMsi.IsEnabled = true;
            BtnExportStandard.IsEnabled = true;
        }
        catch (Exception ex)
        {
            ClientLogger.Error($"Load failed: {ex.Message}");
            MessageBox.Show($"Greška pri učitavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportStandard_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;

        var sfd = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp|GIF (*.gif)|*.gif"
        };
        if (sfd.ShowDialog() != true) return;

        BitmapEncoder encoder = Path.GetExtension(sfd.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };

        using var stream = File.Create(sfd.FileName);
        encoder.Frames.Add(BitmapFrame.Create(_current));
        encoder.Save(stream);
        ClientLogger.Info($"Exported {sfd.FileName}");
        MessageBox.Show("Slika sačuvana.");
    }

    private void BtnExportMsi_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;

        var sfd = new SaveFileDialog { Filter = "MSI (*.msi)|*.msi" };
        if (sfd.ShowDialog() != true) return;

        using var bmp = BitmapImageToBitmap(_current);
        byte[] msiBytes = MsiEncoder.Encode(bmp);
        File.WriteAllBytes(sfd.FileName, msiBytes);
        ClientLogger.Info($"Exported MSI {sfd.FileName} size={msiBytes.Length}B");
        MessageBox.Show("MSI sačuvan.");
    }

    #endregion

    #region History

    private void SetImage(BitmapImage img, bool resetHistory = false)
    {
        if (!resetHistory && _current != null)
        {
            _undo.Add(CloneBitmap(_current));
            if (_undo.Count > MaxHistory)
                _undo.RemoveAt(0);
            _redo.Clear();
        }

        _current = img;
        ImgDisplay.Source = img;
        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        BtnUndo.IsEnabled = _undo.Count > 0;
        BtnRedo.IsEnabled = _redo.Count > 0;
    }

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0 || _current == null) return;
        _redo.Add(CloneBitmap(_current));
        _current = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        ImgDisplay.Source = _current;
        UpdateHistoryButtons();
    }

    private void BtnRedo_Click(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0 || _current == null) return;
        _undo.Add(CloneBitmap(_current));
        if (_undo.Count > MaxHistory) _undo.RemoveAt(0);
        _current = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        ImgDisplay.Source = _current;
        UpdateHistoryButtons();
    }

    #endregion

    #region Compare

    private void ChkCompare_Changed(object sender, RoutedEventArgs e)
    {
        bool compare = ChkCompare.IsChecked == true;
        PanelOriginal.Visibility = compare ? Visibility.Visible : Visibility.Collapsed;
        PanelResult.SetValue(System.Windows.Controls.Grid.ColumnProperty, compare ? 1 : 0);
        PanelResult.SetValue(System.Windows.Controls.Grid.ColumnSpanProperty, compare ? 1 : 2);
        if (compare && _original != null)
            ImgOriginal.Source = _original;
    }

    #endregion

    #region Filters

    private async void BtnFilter_Click(object sender, RoutedEventArgs e)
    {
        string filter = (string)((FrameworkElement)sender).Tag;
        await SendToServer(new List<FilterAction> { BuildFilterAction(filter) });
    }

    private async void BtnBrightness_Click(object sender, RoutedEventArgs e)
    {
        await SendToServer(new List<FilterAction> { BuildFilterAction("Brightness") });
    }

    private void BtnAddQueue_Click(object sender, RoutedEventArgs e)
    {
        if (CmbQueueFilter.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            FilterQueue.Items.Add(item.Content.ToString());
    }

    private async void BtnApplyChain_Click(object sender, RoutedEventArgs e)
    {
        var actions = new List<FilterAction>();
        foreach (var item in FilterQueue.Items)
        {
            string name = item.ToString()!;
            string tag = name switch
            {
                "Invert" => "Invert",
                "Brightness" => "Brightness",
                "EdgeEnhance" => "EdgeEnhance",
                "Emboss Laplacian" => "EmbossLaplacian",
                "Pixelate" => "Pixelate",
                "Time Warp" => "TimeWarp",
                "JJN Dithering" => "JjnDithering",
                "Colorize Gray" => "Colorize",
                _ => name.Replace(" ", "")
            };
            actions.Add(BuildFilterAction(tag));
        }
        if (actions.Count == 0) return;
        await SendToServer(actions);
        FilterQueue.Items.Clear();
    }

    private FilterAction BuildFilterAction(string filterName)
    {
        var action = new FilterAction
        {
            Filter = Enum.Parse<FilterType>(filterName, ignoreCase: true),
            Parameters = new Dictionary<string, object>()
        };

        switch (action.Filter)
        {
            case FilterType.Brightness:
                action.Parameters["amount"] = (int)SliderBrightness.Value;
                break;
            case FilterType.EdgeEnhance:
                action.Parameters["threshold"] = (int)SliderEdge.Value;
                break;
            case FilterType.Pixelate:
                action.Parameters["pixel"] = (int)SliderPixelate.Value;
                break;
            case FilterType.TimeWarp:
                action.Parameters["factor"] = SliderTimeWarp.Value;
                break;
            case FilterType.Colorize:
                action.Parameters["r"] = (int)SliderColorR.Value;
                action.Parameters["g"] = (int)SliderColorG.Value;
                action.Parameters["b"] = (int)SliderColorB.Value;
                break;
        }
        return action;
    }

    private async Task SendToServer(List<FilterAction> filters)
    {
        if (_current == null) return;

        try
        {
            byte[] imageBytes = ImageToPngBytes(_current);
            var request = new ApplyFiltersRequest
            {
                Image = imageBytes,
                ImageId = _sessionId,
                Filters = filters
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var json = JsonSerializer.Serialize(request);
            var response = await Http.PostAsync("api/image/process",
                new StringContent(json, Encoding.UTF8, "application/json"));
            sw.Stop();

            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                ClientLogger.Error($"Process failed: {responseJson}");
                MessageBox.Show("Server greška – pogledaj log.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = JsonSerializer.Deserialize<ApiResponse<ApplyFiltersResponse>>(responseJson, JsonOptions());
            if (result?.Data?.Image == null)
            {
                MessageBox.Show("Prazan odgovor servera.");
                return;
            }

            SetImage(BytesToBitmapImage(result.Data.Image));
            TxtStatus.Text = $"Obrada: {result.Data.TotalProcessingTimeMs} ms (mreža {sw.ElapsedMilliseconds} ms)";
            ClientLogger.Info($"Process filters=[{string.Join(",", filters.Select(f => f.Filter))}] server={result.Data.TotalProcessingTimeMs}ms");
        }
        catch (Exception ex)
        {
            ClientLogger.Error($"Process exception: {ex.Message}");
            MessageBox.Show($"Greška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Zoom / Pan

    private void ImgDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _zoom += e.Delta > 0 ? 0.1 : -0.1;
        _zoom = Math.Clamp(_zoom, 0.2, 5);
        ApplyTransform();
    }

    private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
    {
        _zoom = 1;
        ImgTranslate.X = ImgTranslate.Y = 0;
        OriginalTranslate.X = OriginalTranslate.Y = 0;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        ImgScale.ScaleX = ImgScale.ScaleY = _zoom;
        OriginalScale.ScaleX = OriginalScale.ScaleY = _zoom;
    }

    private void Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _panStart = e.GetPosition(this);
        _panStartX = ImgTranslate.X;
        _panStartY = ImgTranslate.Y;
        ((UIElement)sender).CaptureMouse();
    }

    private void Img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        ((UIElement)sender).ReleaseMouseCapture();
    }

    private void Img_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(this);
        double dx = pos.X - _panStart.X;
        double dy = pos.Y - _panStart.Y;
        ImgTranslate.X = _panStartX + dx;
        ImgTranslate.Y = _panStartY + dy;
        OriginalTranslate.X = _panStartX + dx;
        OriginalTranslate.Y = _panStartY + dy;
    }

    #endregion

    #region Image helpers

    private static JsonSerializerOptions JsonOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    //kad saljem serveru sliku saljem u bytes
    private static byte[] ImageToPngBytes(BitmapImage img)
    {
        using var ms = new MemoryStream();
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(img));
        enc.Save(ms);
        return ms.ToArray();
    }

    //kad dobijam sliku iz bytes pretvaram u BitmapImage
    private static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }

    private static BitmapImage CloneBitmap(BitmapImage source)
    {
        return BytesToBitmapImage(ImageToPngBytes(source));
    }

    //ovo samo kad radim s msi jer decoder vrati Bitmap a meni u cleint treba image
    private static BitmapImage BitmapToBitmapImage(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return BytesToBitmapImage(ms.ToArray());
    }

    //kod export msi jer encoder ocekuje bitmapu
    private static Bitmap BitmapImageToBitmap(BitmapImage img)
    {
        using var ms = new MemoryStream(ImageToPngBytes(img));
        return new Bitmap(ms);
    }

    #endregion
}
