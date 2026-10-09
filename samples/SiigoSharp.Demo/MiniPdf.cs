using System.Text;

namespace SiigoSharp.Demo;

/// <summary>Writes a one-page PDF with lines of text, enough for the simulated invoice.</summary>
internal static class MiniPdf
{
    public static byte[] Create(IEnumerable<string> lines)
    {
        var text = new StringBuilder("BT /F1 12 Tf 16 TL 60 780 Td\n");
        foreach (var line in lines)
        {
            text.Append('(').Append(Escape(line)).Append(") Tj T*\n");
        }

        text.Append("ET");

        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(text.ToString())} >>\nstream\n{text}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
        ];

        // Latin-1 writes one byte per character, so string lengths are byte offsets for the cross-reference table.
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var crossReference = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append($"{offset:D10} 00000 n \n");
        }

        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{crossReference}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    private static string Escape(string line) =>
        line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
