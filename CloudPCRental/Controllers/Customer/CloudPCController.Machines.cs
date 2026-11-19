using Microsoft.AspNetCore.Mvc;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// โมดูลค้นหาและคัดกรองเครื่องคอมพิวเตอร์ (Machines Catalog)
    /// </summary>
    public partial class CloudPCController : Controller
    {
        // ==========================================
        // 2. ค้นหาและคัดกรองเครื่อง (Machines Catalog) - Wireframe Page 3
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Machines(string? search, string? category, string? status, decimal? minPrice, decimal? maxPrice, string? sort, int page = 1)
        {
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Status = status;
            ViewBag.MinPrice = minPrice ?? 5;
            ViewBag.MaxPrice = maxPrice ?? 100;
            ViewBag.Sort = sort ?? "available_first";

            var allMachines = await GetAllMachinesAsync();

            // นับจำนวนตามสถานะจริงเพื่อแสดงบน Sidebar Radio
            ViewBag.CountAvailable = allMachines.Count(m => m.Status == "Available");
            ViewBag.CountRented = allMachines.Count(m => m.Status == "Rented");
            ViewBag.CountMaintenance = allMachines.Count(m => m.Status == "Maintenance");

            var query = allMachines.AsEnumerable();

            // ค้นหาตามคำ
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.MachineName.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                         m.CPU.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                         m.GPU.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // กรองตามสถานะ (ว่าง, กำลังใช้งาน, ซ่อมบำรุง)
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(m => m.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            // กรองตามช่วงราคา
            if (minPrice.HasValue)
            {
                query = query.Where(m => m.PricePerHour >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(m => m.PricePerHour <= maxPrice.Value);
            }

            // จัดเรียง
            if (sort == "price_asc")
            {
                query = query.OrderBy(m => m.PricePerHour);
            }
            else if (sort == "price_desc")
            {
                query = query.OrderByDescending(m => m.PricePerHour);
            }
            else
            {
                // เครื่องว่างอันดับแรก
                query = query.OrderBy(m => m.Status == "Available" ? 0 : m.Status == "Rented" ? 1 : 2);
            }

            var fullList = query.ToList();
            int totalCount = fullList.Count;

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = 1;
            ViewBag.TotalPages = 1;

            return View(fullList);
        }
    }
}
