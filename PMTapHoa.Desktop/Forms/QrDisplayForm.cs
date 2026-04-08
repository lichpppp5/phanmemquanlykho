namespace PMTapHoa.Desktop.Forms;

public class QrDisplayForm : Form
{
    private readonly PictureBox _picture;
    private readonly Label _lblAmount;
    private readonly Label _lblContent;
    private readonly Label _lblGuide;
    private readonly Button _btnConfirmed;
    private readonly Button _btnCancel;

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

        _lblGuide = new Label
        {
            Text = "Sau khi khách thanh toán xong, bấm 'Đã xác nhận thanh toán'.",
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            ForeColor = Color.DimGray,
            AutoSize = true,
            Location = new Point(30, 105)
        };

        _picture = new PictureBox
        {
            Image = qrImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(30, 150),
            Size = new Size(620, 620)
        };

        _btnConfirmed = new Button
        {
            Text = "Đã xác nhận thanh toán",
            Width = 280,
            Height = 52,
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnConfirmed.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        _btnCancel = new Button
        {
            Text = "Hủy",
            Width = 150,
            Height = 52,
            BackColor = UiStyle.Danger,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(_lblAmount);
        Controls.Add(_lblContent);
        Controls.Add(_lblGuide);
        Controls.Add(_picture);
        Controls.Add(_btnConfirmed);
        Controls.Add(_btnCancel);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }

    public DialogResult ShowOnBestScreen(IWin32Window? owner = null)
    {
        var screens = Screen.AllScreens;
        var target = screens.Length > 1 ? screens[1] : screens[0];
        Bounds = target.Bounds;

        var pictureSize = Math.Min(target.Bounds.Width - 120, target.Bounds.Height - 330);
        _picture.Size = new Size(pictureSize, pictureSize);
        _picture.Location = new Point((target.Bounds.Width - pictureSize) / 2, 150);
        _lblAmount.Location = new Point(30, 20);
        _lblContent.Location = new Point(30, 70);
        _lblGuide.Location = new Point(30, 110);

        var buttonY = _picture.Bottom + 12;
        var centerX = target.Bounds.Width / 2;
        _btnConfirmed.Location = new Point(centerX - _btnConfirmed.Width - 10, buttonY);
        _btnCancel.Location = new Point(centerX + 10, buttonY);

        return owner == null ? ShowDialog() : ShowDialog(owner);
    }
}
