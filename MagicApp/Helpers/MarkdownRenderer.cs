using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Markdig;
using System;

namespace MagicApp.Services
{
    public static class MarkdownRenderer
    {
        // 创建支持“单换行换行”的管道
        private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();

        /// <summary>
        /// 生成完整的 HTML 页面字符串
        /// </summary>
        public static string BuildHtmlPage(string markdown, string title, ElementTheme theme)
        {
            // 1. 转换 Markdown 为 HTML
            string contentHtml = string.IsNullOrWhiteSpace(markdown)
                ? "<p><em>No content provided.</em></p>"
                : Markdown.ToHtml(markdown, _pipeline);

            // 2. 主题颜色
            bool isDark = theme == ElementTheme.Dark;
            string bgColor = isDark ? "#2B2B2B" : "#FFFFFF";
            string textColor = isDark ? "#FFFFFF" : "#000000";
            string codeBg = isDark ? "#2D2D30" : "#F5F5F5";
            string borderColor = isDark ? "#3D3D40" : "#E1E1E1";
            string blockquoteBg = isDark ? "rgba(0, 120, 212, 0.1)" : "rgba(0, 120, 212, 0.05)";
            string linkColor = "#0078D4";

            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{EscapeHtml(title)}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: {bgColor};
            color: {textColor};
            padding: 16px;
            line-height: 1.6;
            font-size: 14px;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            max-width: 100%;
            overflow-y: auto;
        }}
        .release-header {{
            font-size: 24px;
            font-weight: 600;
            margin-bottom: 15px;
            border-bottom: 1px solid {borderColor};
            padding-bottom: 10px;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 20px;
            margin-bottom: 10px;
            color: {textColor};
        }}
        h1 {{ font-size: 20px; }}
        h2 {{ font-size: 18px; }}
        h3 {{ font-size: 16px; }}
        h4 {{ font-size: 14px; }}
        a {{
            color: {linkColor};
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        p {{
            margin: 8px 0;
        }}
        code {{
            background-color: {codeBg};
            padding: 2px 4px;
            border-radius: 3px;
            font-family: 'Cascadia Mono', Consolas, 'Courier New', monospace;
            font-size: 12px;
        }}
        pre {{
            background-color: {codeBg};
            padding: 12px;
            border-radius: 5px;
            overflow-x: auto;
            border: 1px solid {borderColor};
            font-size: 12px;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
            border-radius: 0;
            font-size: inherit;
        }}
        blockquote {{
            border-left: 4px solid {linkColor};
            margin: 10px 0;
            padding: 8px 12px;
            background-color: {blockquoteBg};
            color: {(isDark ? "#cccccc" : "#333333")};
        }}
        ul, ol {{
            margin: 8px 0;
            padding-left: 24px;
        }}
        li {{
            margin: 4px 0;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 12px 0;
        }}
        th, td {{
            border: 1px solid {borderColor};
            padding: 8px 12px;
            text-align: left;
        }}
        th {{
            background-color: {codeBg};
            font-weight: 600;
        }}
        hr {{
            border: none;
            border-top: 1px solid {borderColor};
            margin: 20px 0;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
    </style>
</head>
<body>
    <div class='release-header'>{EscapeHtml(title)}</div>
    {contentHtml}
</body>
</html>";

            return html;
        }

        // 直接加载 Markdown 到 WebView2
        public static async void LoadMarkdown(WebView2 webView, string markdown, string title, ElementTheme theme)
        {
            if (webView == null) return;

            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async();
            }

            string html = BuildHtmlPage(markdown, title, theme);
            webView.CoreWebView2?.NavigateToString(html);
        }

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}