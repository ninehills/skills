// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using DocumentFormat.OpenXml.Packaging;

namespace OfficeCli.Core;

/// <summary>
/// Resolves image sources from file paths, data URIs, or HTTP(S) URLs into a stream and content type.
/// Supports:
///   - Local file path: "/tmp/logo.png", "C:\images\photo.jpg"
///   - Data URI: "data:image/png;base64,iVBOR..."
///   - HTTP(S) URL: "https://example.com/image.png"
///
/// Returns a content type string compatible with OpenXmlPart.AddImagePart() (e.g. ImagePartType.Png).
/// </summary>
internal static class ImageSource
{
    /// <summary>
    /// Resolve an image source string into a stream and content type string.
    /// Caller is responsible for disposing the returned stream.
    /// The returned contentType can be passed directly to AddImagePart().
    /// </summary>
    public static (Stream Stream, PartTypeInfo ContentType) Resolve(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Image source cannot be empty");

        // Data URI: data:image/png;base64,iVBOR...
        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return ResolveDataUri(source);

        // HTTP(S) URL
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return ResolveUrl(source);

        // Local file path
        return ResolveFile(source);
    }

    /// <summary>
    /// Determine content type string from a file extension (with or without dot).
    /// Returns a value usable with AddImagePart().
    /// </summary>
    public static PartTypeInfo ExtensionToContentType(string extension)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "png" => ImagePartType.Png,
            "jpg" or "jpeg" => ImagePartType.Jpeg,
            "gif" => ImagePartType.Gif,
            "bmp" => ImagePartType.Bmp,
            "tif" or "tiff" => ImagePartType.Tiff,
            "emf" => ImagePartType.Emf,
            "wmf" => ImagePartType.Wmf,
            "svg" => ImagePartType.Svg,
            _ => throw new ArgumentException($"Unsupported image format: .{ext}. Supported: png, jpg, gif, bmp, tiff, emf, wmf, svg")
        };
    }

    private static (Stream, PartTypeInfo) ResolveFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Image file not found: {path}");

        var contentType = ExtensionToContentType(Path.GetExtension(path));
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

        // Magic-byte validation for raster formats. SVG (XML) / EMF / WMF are
        // intentionally skipped: SVG has no fixed magic, EMF/WMF have weaker
        // headers and TrySniffContentType doesn't cover them. Only validate
        // formats whose first 4 bytes are stable (png/jpg/gif/bmp/tiff).
        var rasterExts = new[] { "png", "jpg", "jpeg", "gif", "bmp", "tif", "tiff" };
        if (rasterExts.Contains(ext))
        {
            var bytes = File.ReadAllBytes(path);
            if (TrySniffContentType(bytes, out var sniffed))
            {
                if (!IsCompatible(sniffed, contentType))
                    throw new ArgumentException(
                        $"Image file '{path}' has extension .{ext} but magic bytes indicate {ContentTypeName(sniffed)}. " +
                        "Rename or convert the file.");
            }
            else
            {
                throw new ArgumentException(
                    $"Image file '{path}' does not appear to be a valid {ext} file (magic bytes mismatch).");
            }
            return (new MemoryStream(bytes, writable: false), contentType);
        }

        return (File.OpenRead(path), contentType);
    }

    private static bool IsCompatible(PartTypeInfo sniffed, PartTypeInfo declared)
    {
        if (sniffed == declared) return true;
        // jpg/jpeg are the same PartTypeInfo so this collapses naturally.
        return false;
    }

    private static string ContentTypeName(PartTypeInfo type)
    {
        if (type == ImagePartType.Png) return "PNG";
        if (type == ImagePartType.Jpeg) return "JPEG";
        if (type == ImagePartType.Gif) return "GIF";
        if (type == ImagePartType.Bmp) return "BMP";
        if (type == ImagePartType.Tiff) return "TIFF";
        return type.ContentType ?? "unknown";
    }

    private static (Stream, PartTypeInfo) ResolveDataUri(string dataUri)
    {
        // Format: data:[<mediatype>][;base64],<data>
        var commaIdx = dataUri.IndexOf(',');
        if (commaIdx < 0)
            throw new ArgumentException("Invalid data URI: missing comma separator");

        var header = dataUri[..commaIdx]; // e.g. "data:image/png;base64"
        var data = dataUri[(commaIdx + 1)..];

        if (!header.Contains("base64", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only base64-encoded data URIs are supported");

        // Extract MIME type
        var mimeStart = header.IndexOf(':') + 1;
        var mimeEnd = header.IndexOf(';');
        var mime = mimeEnd > mimeStart ? header[mimeStart..mimeEnd] : header[mimeStart..];

        var contentType = MimeToContentType(mime);
        var bytes = Convert.FromBase64String(data);
        return (new MemoryStream(bytes), contentType);
    }

    // Cap on a fetched image to bound memory use on a hostile/huge response.
    private const long MaxRemoteImageBytes = 100L * 1024 * 1024; // 100 MB

    private static (Stream, PartTypeInfo) ResolveUrl(string url)
    {
        // SSRF guard: validate the *actual* IP at connect time, for every
        // connection including each redirect hop. Redirects stay enabled so
        // legitimate public CDNs that 30x still work, but no hop is allowed to
        // land on a loopback / private / link-local address (cloud metadata,
        // localhost services, intranet hosts). Validating in ConnectCallback —
        // rather than resolving the hostname up front — also closes the
        // DNS-rebinding/TOCTOU window, since the address we vet is the address
        // we connect to.
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            ConnectCallback = async (ctx, ct) =>
            {
                var host = ctx.DnsEndPoint.Host;
                var addresses = await Dns.GetHostAddressesAsync(host, ct).ConfigureAwait(false);
                foreach (var addr in addresses)
                {
                    if (!IsPublicAddress(addr))
                        throw new ArgumentException(
                            $"Refusing to fetch image from non-public address '{addr}' (host '{host}'). " +
                            "Remote image sources must resolve to a public IP (SSRF protection).");
                }
                var target = addresses.FirstOrDefault()
                    ?? throw new ArgumentException($"Could not resolve host '{host}'.");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(target, ctx.DnsEndPoint.Port, ct).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        using var client = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Add("User-Agent", "OfficeCLI");

        var response = client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        // Enforce the size cap whether or not the server sends Content-Length.
        var declared = response.Content.Headers.ContentLength;
        if (declared is > MaxRemoteImageBytes)
            throw new ArgumentException($"Remote image exceeds {MaxRemoteImageBytes / (1024 * 1024)} MB limit.");
        var bytes = ReadBounded(response.Content.ReadAsStream(), MaxRemoteImageBytes, url);
        var stream = new MemoryStream(bytes);

        // Try content-type header first
        var serverMime = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrEmpty(serverMime) && TryMimeToContentType(serverMime, out var ct))
            return (stream, ct);

        // Fallback: extract extension from URL path (strip query string)
        var uri = new Uri(url);
        var ext = Path.GetExtension(uri.AbsolutePath);
        if (!string.IsNullOrEmpty(ext))
            return (stream, ExtensionToContentType(ext));

        // Last resort: sniff magic bytes
        if (TrySniffContentType(bytes, out var sniffed))
            return (stream, sniffed);

        throw new ArgumentException($"Cannot determine image type from URL: {url}. Specify format via file extension or content-type header.");
    }

    private static byte[] ReadBounded(Stream src, long max, string url)
    {
        using var ms = new MemoryStream();
        var buf = new byte[81920];
        long total = 0;
        int n;
        while ((n = src.Read(buf, 0, buf.Length)) > 0)
        {
            total += n;
            if (total > max)
                throw new ArgumentException($"Remote image at {url} exceeds {max / (1024 * 1024)} MB limit.");
            ms.Write(buf, 0, n);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// True only for globally-routable addresses. Blocks loopback, private
    /// (RFC1918), link-local (incl. 169.254.0.0/16 cloud-metadata), unique-local
    /// IPv6 (fc00::/7), multicast and unspecified — the SSRF target ranges.
    /// </summary>
    internal static bool IsPublicAddress(IPAddress address)
    {
        var addr = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        if (IPAddress.IsLoopback(addr)) return false;
        if (addr.Equals(IPAddress.Any) || addr.Equals(IPAddress.IPv6Any)) return false;

        if (addr.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = addr.GetAddressBytes(); // big-endian
            if (b[0] == 10) return false;                                   // 10.0.0.0/8
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;      // 172.16.0.0/12
            if (b[0] == 192 && b[1] == 168) return false;                   // 192.168.0.0/16
            if (b[0] == 169 && b[1] == 254) return false;                   // 169.254.0.0/16 link-local (cloud metadata)
            if (b[0] == 127) return false;                                  // 127.0.0.0/8
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false;     // 100.64.0.0/10 CGNAT
            if (b[0] == 0) return false;                                    // 0.0.0.0/8
            if (b[0] >= 224) return false;                                  // 224.0.0.0/4 multicast + 240/4 reserved
            return true;
        }

        if (addr.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal || addr.IsIPv6Multicast) return false;
            var b = addr.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return false;                        // fc00::/7 unique-local
            return true;
        }

        return false;
    }

    private static PartTypeInfo MimeToContentType(string mime)
    {
        if (TryMimeToContentType(mime, out var ct)) return ct;
        throw new ArgumentException($"Unsupported MIME type: {mime}. Supported: image/png, image/jpeg, image/gif, image/bmp, image/tiff, image/svg+xml");
    }

    private static bool TryMimeToContentType(string mime, out PartTypeInfo contentType)
    {
        contentType = mime.ToLowerInvariant() switch
        {
            "image/png" => ImagePartType.Png,
            "image/jpeg" or "image/jpg" => ImagePartType.Jpeg,
            "image/gif" => ImagePartType.Gif,
            "image/bmp" => ImagePartType.Bmp,
            "image/tiff" or "image/tif" => ImagePartType.Tiff,
            "image/svg+xml" => ImagePartType.Svg,
            "image/emf" or "image/x-emf" => ImagePartType.Emf,
            "image/wmf" or "image/x-wmf" => ImagePartType.Wmf,
            _ => default
        };
        return contentType != default;
    }

    private static bool TrySniffContentType(byte[] bytes, out PartTypeInfo contentType)
    {
        contentType = default;
        if (bytes.Length < 4) return false;

        // PNG: 89 50 4E 47
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        { contentType = ImagePartType.Png; return true; }

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        { contentType = ImagePartType.Jpeg; return true; }

        // GIF: GIF8
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
        { contentType = ImagePartType.Gif; return true; }

        // BMP: BM
        if (bytes[0] == 0x42 && bytes[1] == 0x4D)
        { contentType = ImagePartType.Bmp; return true; }

        // TIFF little-endian: 49 49 2A 00 ("II" + magic 42)
        if (bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00)
        { contentType = ImagePartType.Tiff; return true; }

        // TIFF big-endian: 4D 4D 00 2A ("MM" + magic 42)
        if (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A)
        { contentType = ImagePartType.Tiff; return true; }

        return false;
    }

    /// <summary>
    /// Try to read pixel (width, height) by parsing image file headers.
    /// Cross-platform — pure byte parsing, no System.Drawing / GDI dependency.
    /// Supports PNG, JPEG, GIF, BMP. Returns null for any unrecognized or
    /// malformed header. The stream position is restored on return.
    /// </summary>
    public static (int Width, int Height)? TryGetDimensions(Stream stream)
    {
        if (stream is null || !stream.CanSeek || stream.Length < 24) return null;

        var startPos = stream.Position;
        try
        {
            stream.Position = 0;
            var header = new byte[30];
            var read = stream.Read(header, 0, header.Length);
            if (read < 24) return null;

            // PNG: signature 89 50 4E 47 0D 0A 1A 0A, IHDR width/height at
            // big-endian offsets 16..19 and 20..23.
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                int w = ReadBE32(header, 16);
                int h = ReadBE32(header, 20);
                return (w > 0 && h > 0) ? (w, h) : null;
            }

            // BMP: signature 42 4D, width little-endian at offset 18, height at 22.
            // Height may be negative for top-down bitmaps; take the absolute value.
            if (header[0] == 0x42 && header[1] == 0x4D && read >= 26)
            {
                int w = ReadLE32(header, 18);
                int h = ReadLE32(header, 22);
                if (h < 0) h = -h;
                return (w > 0 && h > 0) ? (w, h) : null;
            }

            // GIF: signature 47 49 46 38, logical screen width/height are
            // little-endian uint16 at offsets 6 and 8.
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
            {
                int w = header[6] | (header[7] << 8);
                int h = header[8] | (header[9] << 8);
                return (w > 0 && h > 0) ? (w, h) : null;
            }

            // JPEG: signature FF D8 — walk markers to find a Start-of-Frame.
            if (header[0] == 0xFF && header[1] == 0xD8)
                return TryGetJpegDimensions(stream);

            // SVG: XML text — sniff for <?xml or <svg in the header and
            // delegate to the SVG parser. Handled after the binary
            // signatures above so SVG files with stray leading whitespace
            // don't get mis-sniffed as PNG/BMP/GIF/JPEG.
            if (LooksLikeSvgHeader(header, read))
                return SvgImageHelper.TryGetSvgDimensions(stream);

            return null;
        }
        catch (IOException)
        {
            return null;
        }
        finally
        {
            try { stream.Position = startPos; } catch (IOException) { /* best effort */ }
        }
    }

    private static bool LooksLikeSvgHeader(byte[] header, int read)
    {
        if (header is null || read < 4) return false;
        int i = 0;
        // UTF-8 BOM
        if (read >= 3 && header[0] == 0xEF && header[1] == 0xBB && header[2] == 0xBF) i = 3;
        while (i < read && (header[i] == ' ' || header[i] == '\t'
            || header[i] == '\r' || header[i] == '\n')) i++;
        if (i >= read || header[i] != (byte)'<') return false;
        var text = System.Text.Encoding.UTF8.GetString(header, i, read - i).ToLowerInvariant();
        return text.StartsWith("<svg") || text.StartsWith("<?xml") || text.StartsWith("<!doctype svg");
    }

    private static int ReadBE32(byte[] buf, int offset) =>
        (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];

    private static int ReadLE32(byte[] buf, int offset) =>
        buf[offset] | (buf[offset + 1] << 8) | (buf[offset + 2] << 16) | (buf[offset + 3] << 24);

    private static (int Width, int Height)? TryGetJpegDimensions(Stream stream)
    {
        // Skip the SOI marker (FF D8) and walk segment markers looking for
        // a Start-of-Frame (SOFn) marker, which holds the true pixel size.
        stream.Position = 2;
        var buf = new byte[7];

        while (stream.Position < stream.Length - 2)
        {
            int b1 = stream.ReadByte();
            if (b1 != 0xFF) return null;

            int b2;
            do
            {
                b2 = stream.ReadByte();
            } while (b2 == 0xFF && stream.Position < stream.Length);
            if (b2 < 0) return null;

            // SOFn markers: C0..C3, C5..C7, C9..CB, CD..CF. These all carry
            // the frame header (height then width, each big-endian uint16).
            bool isSof = (b2 >= 0xC0 && b2 <= 0xC3)
                      || (b2 >= 0xC5 && b2 <= 0xC7)
                      || (b2 >= 0xC9 && b2 <= 0xCB)
                      || (b2 >= 0xCD && b2 <= 0xCF);
            if (isSof)
            {
                if (stream.Read(buf, 0, 7) < 7) return null;
                int h = (buf[3] << 8) | buf[4];
                int w = (buf[5] << 8) | buf[6];
                return (w > 0 && h > 0) ? (w, h) : null;
            }

            // Start-of-Scan: image data begins, no more metadata.
            if (b2 == 0xDA) return null;

            // Any other segment: skip over its declared length.
            if (stream.Read(buf, 0, 2) < 2) return null;
            int len = (buf[0] << 8) | buf[1];
            if (len < 2) return null;
            stream.Position += len - 2;
        }
        return null;
    }
}
