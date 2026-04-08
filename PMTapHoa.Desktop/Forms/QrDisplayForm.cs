namespace PMTapHoa.Desktop.Forms;

public class QrDisplayForm : Form
{
    private readonly PictureBox _picture;
    private readonly Label _lblAmount;
    private readonly Label _lblContent;

    public QrDisplayForm(Image qrImage, decimal amount, string transferContent)
    {
        Text = "QR Thanh toán";
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        BackColor = Color.White;

        _lblAmount = new Label
        {
            Text = $"Số tiền: {amount:N0} VND",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = UiStyle.Success,
            AutoSize = true,
            Location = new Point(30, 20)
        };

        _lblContent = new Label
        {
            Text = $"Nội dung CK: {transferContent}",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = UiStyle.AccentSlate,
            AutoSize = true,
            Location = new Point(30, 70)
        };

        _picture = new PictureBox
        {
            Image = qrImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(30, 110),
            Size = new Size(620, 620)
        };

        Controls.Add(_lblAmount);
        Controls.Add(_lblContent);
        Controls.Add(_picture);
    }

    public void ShowOnBestScreen()
    {
        var screens = Screen.AllScreens;
        var target = screens.Length > 1 ? screens[1] : screens[0];
        Bounds = target.Bounds;

        var pictureSize = Math.Min(target.Bounds.Width - 80, target.Bounds.Height - 220);
        _picture.Size = new Size(pictureSize, pictureSize);
        _picture.Location = new Point((target.Bounds.Width - pictureSize) / 2, 120);
        _lblAmount.Location = new Point(30, 20);
        _lblContent.Location = new Point(30, 70);

        Show();
    }
}
