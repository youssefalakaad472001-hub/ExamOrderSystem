"""
Builds ProjectDocumentation.pdf (Arabic, right-to-left) from ProjectDocumentation.md.

How to use:
    pip install markdown pygments
    python Documentation/build_pdf.py

Needs Microsoft Edge (it comes with Windows) to print the PDF,
and an internet connection, because the Arabic font "IBM Plex Sans Arabic"
is loaded from Google Fonts.
"""
import pathlib
import subprocess
import sys
import tempfile

try:
    import markdown
    from pygments.formatters import HtmlFormatter
except ImportError:
    sys.exit("Please run first:  pip install markdown pygments")

# The folder of this script, and the input and output files next to it
HERE = pathlib.Path(__file__).resolve().parent
MD_FILE = HERE / "ProjectDocumentation.md"
PDF_FILE = HERE / "ProjectDocumentation.pdf"

# Where Microsoft Edge is usually installed
EDGE_PATHS = [
    pathlib.Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
    pathlib.Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
]

# The .md file wraps the text in <div dir="rtl"> and every code block in <div dir="ltr">,
# so it shows right-to-left in VS Code and GitHub. The PDF sets the direction with CSS instead.
WRAPPER_LINES = {'<div dir="rtl">', '<div dir="ltr">', "</div>"}

CSS = """
@page {
    size: A4;
    margin: 18mm 15mm 18mm 15mm;
    @bottom-center {
        content: counter(page) " / " counter(pages);
        font-family: "IBM Plex Sans Arabic", "Segoe UI", sans-serif;
        font-size: 9pt;
        color: #8a8f98;
    }
    @top-left {
        content: "SalesOrderManagement (ExamRFID)";
        font-family: "IBM Plex Sans Arabic", "Segoe UI", sans-serif;
        font-size: 8pt;
        color: #a0a4ab;
    }
}
@page :first {
    @bottom-center { content: none; }
    @top-left { content: none; }
}
html { direction: rtl; }
body {
    margin: 0;
    font-family: "IBM Plex Sans Arabic", "Segoe UI", Tahoma, sans-serif;
    font-size: 10.5pt;
    line-height: 1.8;
    letter-spacing: normal;
    color: #1f2328;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
}
h1 { font-size: 25pt; line-height: 1.35; margin: 35mm 0 4mm; color: #111; }
h1 + p { font-size: 12.5pt; color: #3d444d; margin-bottom: 10mm; }
h2 {
    break-before: page;
    margin: 0 0 12px;
    padding-bottom: 5px;
    font-size: 18pt;
    line-height: 1.4;
    color: #14361a;
    border-bottom: 3px solid #8be36b;
}
h3 { font-size: 13.5pt; line-height: 1.45; margin: 20px 0 6px; color: #1a1a1a; break-after: avoid; }
h4 { font-size: 11.5pt; margin: 14px 0 4px; break-after: avoid; }
p { margin: 6px 0 8px; }
em { font-style: normal; }
strong { font-weight: 700; }
ul, ol { padding-inline-start: 24px; margin: 6px 0 10px; }
li { margin: 2px 0; }
hr { display: none; }
a { color: #1a7f37; text-decoration: none; }

/* Code font. Arabic comments inside code fall back to the Arabic font (never Courier New) */
code, pre { font-family: "Cascadia Mono", Consolas, "IBM Plex Sans Arabic", monospace; }

/* Inline code inside Arabic text: always left-to-right and isolated from the Arabic around it.
   break-word (not anywhere) so table columns stay wide enough for whole names like ProductName */
:not(pre) > code {
    direction: ltr;
    unicode-bidi: isolate;
    padding: 0 4px;
    border-radius: 4px;
    background: #eef1f4;
    color: #24292f;
    font-size: 0.86em;
    white-space: nowrap;
}

/* Inside tables, long code may wrap (but only between whole words) */
td code, th code {
    white-space: normal;
    overflow-wrap: break-word;
}

/* Pygments makes comments italic; italic Arabic looks broken, so keep them upright */
.codehilite .c, .codehilite .c1, .codehilite .cm, .codehilite .cs,
.codehilite .ch, .codehilite .cp, .codehilite .cpf { font-style: normal !important; }

/* Pygments draws a red box around text it doesn't understand, like the "..." we use to shorten code */
.codehilite .err { border: none !important; color: inherit !important; }

/* Code blocks: left-to-right, left-aligned */
.codehilite, pre {
    direction: ltr;
    text-align: left;
}
.codehilite {
    margin: 8px 0 12px;
    border: 1px solid #d8dee4;
    border-radius: 6px;
    background: #f6f8fa !important;
    break-inside: avoid;
}
.codehilite pre, pre {
    margin: 0;
    padding: 9px 12px;
    font-size: 8.5pt;
    line-height: 1.5;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
    background: transparent;
}

table {
    width: 100%;
    margin: 10px 0 14px;
    border-collapse: collapse;
    font-size: 9.3pt;
    line-height: 1.6;
}
thead { display: table-header-group; }
tr { break-inside: avoid; }
th, td {
    padding: 5px 8px;
    border: 1px solid #d0d7de;
    text-align: start;
    vertical-align: top;
}
th { background: #eef6ea; font-weight: 700; }

blockquote {
    margin: 12px 0;
    padding: 6px 14px;
    border-inline-start: 4px solid #6cc04a;
    border-radius: 4px;
    background: #f3faef;
}
blockquote p { margin: 4px 0; }
"""


def strip_direction_wrappers(text: str) -> str:
    """Removes the <div dir=...> lines that are outside code blocks."""
    result = []
    in_code = False
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("```"):
            in_code = not in_code
        elif not in_code and stripped in WRAPPER_LINES:
            continue
        result.append(line)
    return "\n".join(result)


def main() -> None:
    # Find Edge
    edge = next((p for p in EDGE_PATHS if p.exists()), None)
    if edge is None:
        sys.exit("Microsoft Edge was not found.")

    # Markdown -> HTML (code blocks get syntax colors from Pygments)
    text = strip_direction_wrappers(MD_FILE.read_text(encoding="utf-8"))
    body = markdown.markdown(
        text,
        extensions=["extra", "codehilite", "sane_lists"],
        extension_configs={"codehilite": {"guess_lang": False, "css_class": "codehilite"}},
        output_format="html5",
    )
    code_colors = HtmlFormatter(style="friendly").get_style_defs(".codehilite")
    html = f"""<!doctype html>
<html lang="ar" dir="rtl">
<head>
<meta charset="utf-8">
<title>توثيق مشروع SalesOrderManagement</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Arabic:wght@400;500;600;700&display=swap">
<style>{code_colors}
{CSS}</style>
</head>
<body>
{body}
</body>
</html>"""

    # HTML -> PDF with Edge in headless mode (its own temporary profile, so your open Edge is not touched)
    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as tmp:
        html_file = pathlib.Path(tmp) / "doc.html"
        html_file.write_text(html, encoding="utf-8")
        command = [
            str(edge),
            "--headless=new",
            "--disable-gpu",
            "--no-first-run",
            "--disable-extensions",
            "--no-pdf-header-footer",
            "--virtual-time-budget=20000",
            f"--user-data-dir={pathlib.Path(tmp) / 'edge-profile'}",
            f"--print-to-pdf={PDF_FILE}",
            html_file.as_uri(),
        ]
        subprocess.run(command, check=True, timeout=180, capture_output=True)

    if not PDF_FILE.exists() or PDF_FILE.stat().st_size == 0:
        sys.exit("The PDF was not created.")
    print("PDF created:", PDF_FILE)


if __name__ == "__main__":
    main()
