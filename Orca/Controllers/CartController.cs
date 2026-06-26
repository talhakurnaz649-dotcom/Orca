using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orca.Models;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace Orca.Controllers
{
    public class CartController : Controller
    {
        private readonly OrcaDbContext _context;
        private const string CartKey = "Cart";

        public CartController(OrcaDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartKey);
            return json == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        public IActionResult Add(int gameId)
        {
            var game = _context.Games.FirstOrDefault(g => g.GameId == gameId);
            if (game == null) return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.GameId == gameId);

            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    GameId = game.GameId,
                    Title = game.Title,
                    Price = game.Price,
                    ImageUrl = game.ImageUrl ?? "",
                    Quantity = 1
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int gameId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.GameId == gameId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");
            return View(cart);
        }

        [HttpPost]
        public IActionResult CompleteOrder(string fullName, string email, string address, string cardNumber)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = new Order
            {
                UserId = userId.Value,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                Status = "Tamamlandı",
                OrderDate = DateTime.Now,
                OrderDetails = cart.Select(c => new OrderDetail
                {
                    GameId = c.GameId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            SaveCart(new List<CartItem>());

            TempData["OrderSuccess"] = true;
            TempData["FullName"] = fullName;
            TempData["Email"] = email;
            TempData["Address"] = address;
            TempData["CardNumber"] = cardNumber;
            TempData["OrderId"] = order.OrderId;
            TempData["Total"] = order.TotalAmount.ToString("F2");

            // Ürün bilgilerini JSON olarak sakla
            var items = order.OrderDetails.Select(d => new {
                title = _context.Games.FirstOrDefault(g => g.GameId == d.GameId)?.Title ?? "Oyun",
                quantity = d.Quantity,
                price = d.UnitPrice
            }).ToList();
            TempData["Items"] = JsonSerializer.Serialize(items);

            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        public IActionResult DownloadReceipt()
        {
            var fullName = TempData["FullName"]?.ToString() ?? "";
            var email = TempData["Email"]?.ToString() ?? "";
            var address = TempData["Address"]?.ToString() ?? "";
            var cardNumber = TempData["CardNumber"]?.ToString() ?? "";
            var orderId = TempData["OrderId"]?.ToString() ?? "";
            var total = TempData["Total"]?.ToString() ?? "";
            var itemsJson = TempData["Items"]?.ToString() ?? "[]";

            var items = JsonSerializer.Deserialize<List<JsonElement>>(itemsJson)!;

            var maskedCard = cardNumber.Length >= 4
                ? "**** **** **** " + cardNumber[^4..]
                : "****";

            using var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            doc.SetMargins(30, 40, 30, 40);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var purple = new DeviceRgb(124, 58, 237);
            var darkBg = new DeviceRgb(22, 33, 62);
            var lightPurple = new DeviceRgb(237, 233, 254);
            var green = new DeviceRgb(5, 150, 105);
            var white = ColorConstants.WHITE;
            var textColor = new DeviceRgb(30, 27, 75);
            var borderColor = new DeviceRgb(196, 181, 253);

            // ── HEADER ──
            var headerTable = new Table(1).UseAllAvailableWidth();
            var headerCell = new Cell()
                .SetBackgroundColor(darkBg)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(16)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("ORCA GAMES")
                    .SetFont(bold).SetFontSize(24).SetFontColor(white))
                .Add(new Paragraph("Dijital Oyun Magazasi")
                    .SetFont(normal).SetFontSize(11).SetFontColor(new DeviceRgb(196, 181, 253)));
            headerTable.AddCell(headerCell);
            doc.Add(headerTable);
            doc.Add(new Paragraph("\n").SetFontSize(4));

            // ── ONAY ──
            var onayTable = new Table(1).UseAllAvailableWidth();
            onayTable.AddCell(new Cell()
                .SetBackgroundColor(new DeviceRgb(209, 250, 229))
                .SetBorder(new iText.Layout.Borders.SolidBorder(green, 1.5f))
                .SetPadding(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("ODEME BASARIYLA TAMAMLANDI")
                    .SetFont(bold).SetFontSize(14).SetFontColor(green)));
            doc.Add(onayTable);
            doc.Add(new Paragraph("\n").SetFontSize(6));

            // ── SİPARİŞ BİLGİLERİ ──
            doc.Add(new Paragraph("SIPARIS BILGILERI")
                .SetFont(bold).SetFontSize(10).SetFontColor(purple));

            var infoTable = new Table(new float[] { 3, 5, 3, 5 }).UseAllAvailableWidth();
            void AddInfoRow(string k1, string v1, string k2, string v2, bool alt = false)
            {
                var bg = alt ? new DeviceRgb(245, 243, 255) : lightPurple;
                infoTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).Add(new Paragraph(k1).SetFont(bold).SetFontSize(9).SetFontColor(textColor)));
                infoTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).Add(new Paragraph(v1).SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
                infoTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).Add(new Paragraph(k2).SetFont(bold).SetFontSize(9).SetFontColor(textColor)));
                infoTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).Add(new Paragraph(v2).SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
            }
            AddInfoRow("Siparis No:", "ORK-" + orderId, "Tarih:", DateTime.Now.ToString("dd.MM.yyyy"));
            AddInfoRow("Saat:", DateTime.Now.ToString("HH:mm"), "Durum:", "Odendi", true);
            doc.Add(infoTable);
            doc.Add(new Paragraph("\n").SetFontSize(6));

            // ── MÜŞTERİ BİLGİLERİ ──
            doc.Add(new Paragraph("MUSTERI BILGILERI")
                .SetFont(bold).SetFontSize(10).SetFontColor(purple));

            var musteriTable = new Table(new float[] { 3, 11 }).UseAllAvailableWidth();
            void AddMusteriRow(string key, string value, bool alt = false)
            {
                var bg = alt ? new DeviceRgb(250, 245, 255) : white;
                musteriTable.AddCell(new Cell().SetBackgroundColor(lightPurple).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(8).Add(new Paragraph(key).SetFont(bold).SetFontSize(9).SetFontColor(textColor)));
                musteriTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(8).Add(new Paragraph(value).SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
            }
            AddMusteriRow("Ad Soyad", fullName);
            AddMusteriRow("E-posta", email, true);
            AddMusteriRow("Adres", address);
            AddMusteriRow("Kart", maskedCard, true);
            doc.Add(musteriTable);
            doc.Add(new Paragraph("\n").SetFontSize(6));

            // ── ÜRÜNLER ──
            doc.Add(new Paragraph("SIPARIS DETAYLARI")
                .SetFont(bold).SetFontSize(10).SetFontColor(purple));

            var urunTable = new Table(new float[] { 7, 2, 3, 3 }).UseAllAvailableWidth();
            void AddUrunHeader(string text) =>
                urunTable.AddHeaderCell(new Cell().SetBackgroundColor(purple).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(8).Add(new Paragraph(text).SetFont(bold).SetFontSize(9).SetFontColor(white).SetTextAlignment(TextAlignment.CENTER)));

            AddUrunHeader("Urun"); AddUrunHeader("Adet");
            AddUrunHeader("Birim Fiyat"); AddUrunHeader("Toplam");

            bool altRow = false;
            foreach (var item in items)
            {
                var bg = altRow ? lightPurple : white;
                var title = item.GetProperty("title").GetString() ?? "";
                var qty = item.GetProperty("quantity").GetInt32();
                var price = item.GetProperty("price").GetDecimal();
                var lineTotal = price * qty;

                urunTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).Add(new Paragraph(title).SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
                urunTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(qty.ToString()).SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
                urunTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(price.ToString("F2") + " TL").SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
                urunTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.4f)).SetPadding(7).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(lineTotal.ToString("F2") + " TL").SetFont(normal).SetFontSize(9).SetFontColor(textColor)));
                altRow = !altRow;
            }
            doc.Add(urunTable);
            doc.Add(new Paragraph("\n").SetFontSize(4));

            // ── TOPLAM ──
            decimal totalAmount = decimal.Parse(total);
            decimal kdv = totalAmount * 0.18m;
            decimal araToplam = totalAmount - kdv;

            var toplamTable = new Table(new float[] { 11, 4 }).UseAllAvailableWidth();
            void AddToplamRow(string label, string value, bool isTotal = false)
            {
                var bg = isTotal ? lightPurple : white;
                var font = isTotal ? bold : normal;
                var fc = isTotal ? purple : textColor;
                toplamTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(isTotal ? new iText.Layout.Borders.SolidBorder(borderColor, 1f) : iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(label).SetFont(font).SetFontSize(isTotal ? 11 : 9).SetFontColor(fc)));
                toplamTable.AddCell(new Cell().SetBackgroundColor(bg).SetBorder(isTotal ? new iText.Layout.Borders.SolidBorder(borderColor, 1f) : iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(value).SetFont(font).SetFontSize(isTotal ? 11 : 9).SetFontColor(fc)));
            }
            AddToplamRow("Ara Toplam:", araToplam.ToString("F2") + " TL");
            AddToplamRow("KDV (%18):", kdv.ToString("F2") + " TL");
            AddToplamRow("GENEL TOPLAM:", totalAmount.ToString("F2") + " TL", true);
            doc.Add(toplamTable);
            doc.Add(new Paragraph("\n").SetFontSize(8));

            // ── FOOTER ──
            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f)));
            doc.Add(new Paragraph("Bu belge Orca Games tarafindan otomatik olarak olusturulmustur.")
                .SetFont(normal).SetFontSize(8).SetFontColor(purple).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("0850 123 45 67")
                .SetFont(normal).SetFontSize(8).SetFontColor(purple).SetTextAlignment(TextAlignment.CENTER));

            doc.Close();

            var fileBytes = ms.ToArray();
            return File(fileBytes, "application/pdf", "SiparisDekontu.pdf");
        }
    }
}