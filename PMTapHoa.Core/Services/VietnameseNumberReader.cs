using System.Text;

namespace PMTapHoa.Core.Services;

public static class VietnameseNumberReader
{
    private static readonly string[] Digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
    private static readonly string[] Groups = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

    public static string ToWords(decimal amount, string currencyUnit = "đồng chẵn")
    {
        if (amount == 0) return $"Không {currencyUnit}.";

        var isNegative = amount < 0;
        var number = Math.Abs(Math.Round(amount));

        var groups = new List<long>();
        while (number > 0)
        {
            groups.Add((long)(number % 1000));
            number = Math.Floor(number / 1000);
        }

        var words = new List<string>();
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            var g = groups[i];
            if (g == 0) continue;

            var gText = ReadThreeDigits((int)g, i < groups.Count - 1);
            if (!string.IsNullOrWhiteSpace(gText))
            {
                words.Add(gText);
                if (i > 0 && i < Groups.Length)
                {
                    words.Add(Groups[i]);
                }
            }
        }

        var result = string.Join(" ", words).Trim();
        if (string.IsNullOrWhiteSpace(result)) result = "không";

        if (isNegative) result = "âm " + result;

        result = char.ToUpper(result[0]) + result.Substring(1);
        return $"{result} {currencyUnit}.";
    }

    private static string ReadThreeDigits(int n, bool hasHigherGroups)
    {
        var h = n / 100;
        var t = (n % 100) / 10;
        var u = n % 10;

        var sb = new StringBuilder();

        if (h > 0 || hasHigherGroups)
        {
            sb.Append(Digits[h]).Append(" trăm");
        }

        if (t > 1)
        {
            if (sb.Length > 0) sb.Append(" ");
            sb.Append(Digits[t]).Append(" mươi");
            if (u == 1) sb.Append(" mốt");
            else if (u == 4) sb.Append(" tư");
            else if (u == 5) sb.Append(" lăm");
            else if (u > 0) sb.Append(" ").Append(Digits[u]);
        }
        else if (t == 1)
        {
            if (sb.Length > 0) sb.Append(" ");
            sb.Append("mười");
            if (u == 5) sb.Append(" lăm");
            else if (u > 0) sb.Append(" ").Append(Digits[u]);
        }
        else if (t == 0 && u > 0)
        {
            if (sb.Length > 0) sb.Append(" linh");
            sb.Append(" ").Append(Digits[u]);
        }

        return sb.ToString().Trim();
    }
}
