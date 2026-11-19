// CloudPC JavaScript Helpers

// ฟังก์ชันอัปเดตนาฬิกาเวลาจริงแบบเรียลไทม์ (Live Real-time Clock)
function updateAllLiveClocks() {
    const clockElements = document.querySelectorAll(".realtime-live-clock, #liveRealtimeClock, #customerLiveClock, #adminLiveClock");
    if (!clockElements || clockElements.length === 0) return;

    const thaiMonths = ["ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];
    const now = new Date();
    const d = now.getDate();
    const m = thaiMonths[now.getMonth()];
    const y = now.getFullYear() + 543;
    const hh = String(now.getHours()).padStart(2, '0');
    const mm = String(now.getMinutes()).padStart(2, '0');
    const ss = String(now.getSeconds()).padStart(2, '0');
    const timeString = `${d} ${m} ${y} ${hh}:${mm}:${ss} น.`;

    clockElements.forEach(function (el) {
        el.innerText = timeString;
    });
}

// ฟังก์ชันรีเฟรชข้อมูลหน้าเว็บ พร้อมแอนิเมชันหมุนไอคอน
function refreshPageData(element) {
    if (element) {
        const icon = element.querySelector(".bi-arrow-clockwise") || element;
        if (icon) {
            icon.classList.add("spin-refresh");
        }
    }
    setTimeout(function () {
        window.location.reload();
    }, 350);
}

// จัดการสถานะการอ่านแจ้งเตือนลูกค้า (Customer Notifications)
window.markCustomerNotificationsAsRead = function(e) {
    if (e && e.stopPropagation) {
        e.stopPropagation();
    }
    try {
        localStorage.setItem('cloudpc_customer_notifications_read', 'true');
    } catch (err) { }

    var badge = document.getElementById("customerNotifyBadge");
    if (badge) badge.style.display = "none";

    var headerBadge = document.getElementById("customerNotifyHeaderBadge");
    if (headerBadge) {
        headerBadge.innerText = "0 ใหม่";
        headerBadge.className = "badge bg-secondary-subtle text-secondary border rounded-pill";
    }

    var unreadItems = document.querySelectorAll("#customerNotifyList .unread-item");
    unreadItems.forEach(function(item) {
        item.style.backgroundColor = "#ffffff";
    });

    var notifyDots = document.querySelectorAll("#customerNotifyList .notify-dot");
    notifyDots.forEach(function(dot) {
        dot.style.display = "none";
    });
};

// จัดการสถานะการอ่านแจ้งเตือนแอดมิน (Admin Notifications)
window.markAdminNotificationsAsRead = function(e) {
    if (e && e.stopPropagation) {
        e.stopPropagation();
    }
    try {
        localStorage.setItem('cloudpc_admin_notifications_read', 'true');
    } catch (err) { }

    var badge = document.getElementById("adminNotifyBadge");
    if (badge) badge.style.display = "none";

    var headerBadge = document.getElementById("adminNotifyHeaderBadge");
    if (headerBadge) {
        headerBadge.innerText = "0 ใหม่";
        headerBadge.className = "badge bg-secondary-subtle text-secondary border rounded-pill";
    }

    var unreadItems = document.querySelectorAll("#adminNotifyList .unread-admin-item");
    unreadItems.forEach(function(item) {
        item.style.backgroundColor = "#ffffff";
    });

    var notifyDots = document.querySelectorAll("#adminNotifyList .admin-notify-dot");
    notifyDots.forEach(function(dot) {
        dot.style.display = "none";
    });
};

document.addEventListener("DOMContentLoaded", function () {
    // เริ่มทำงานนาฬิกาเรียลไทม์ทันที และอัปเดตทุก 1 วินาที
    updateAllLiveClocks();
    setInterval(updateAllLiveClocks, 1000);

    // ตรวจสอบและซ่อนแจ้งเตือนถ้าเคยอ่านแล้ว
    if (localStorage.getItem('cloudpc_customer_notifications_read') === 'true') {
        window.markCustomerNotificationsAsRead();
    }
    if (localStorage.getItem('cloudpc_admin_notifications_read') === 'true') {
        window.markAdminNotificationsAsRead();
    }

    // ผูก Event ให้กับปุ่มและดรอปดาวน์แจ้งเตือน
    var custBtn = document.getElementById("customerNotificationBtn");
    if (custBtn) {
        custBtn.addEventListener("click", function() {
            window.markCustomerNotificationsAsRead();
        });
    }
    var custDropdown = document.getElementById("customerNotificationDropdown");
    if (custDropdown) {
        custDropdown.addEventListener("show.bs.dropdown", function() {
            window.markCustomerNotificationsAsRead();
        });
    }
    var custMarkAll = document.getElementById("customerMarkAllReadBtn");
    if (custMarkAll) {
        custMarkAll.addEventListener("click", function(e) {
            window.markCustomerNotificationsAsRead(e);
        });
    }

    var adminBtn = document.getElementById("adminNotificationBtn");
    if (adminBtn) {
        adminBtn.addEventListener("click", function() {
            window.markAdminNotificationsAsRead();
        });
    }
    var adminDropdown = document.getElementById("adminNotificationDropdown");
    if (adminDropdown) {
        adminDropdown.addEventListener("show.bs.dropdown", function() {
            window.markAdminNotificationsAsRead();
        });
    }
    var adminMarkAll = document.getElementById("adminMarkAllReadBtn");
    if (adminMarkAll) {
        adminMarkAll.addEventListener("click", function(e) {
            window.markAdminNotificationsAsRead(e);
        });
    }

    // ผูก Event การคลิกให้กับทุกองค์ประกอบที่มีไอคอน bi-arrow-clockwise หรือ class cursor-pointer
    document.querySelectorAll(".bi-arrow-clockwise").forEach(function (icon) {
        const container = icon.closest(".cursor-pointer") || icon.closest(".refresh-badge") || icon;
        container.style.cursor = "pointer";
        container.setAttribute("title", "คลิกเพื่อรีเฟรชข้อมูลล่าสุด");
        container.classList.add("refresh-badge");

        container.addEventListener("click", function (e) {
            e.preventDefault();
            icon.classList.add("spin-refresh");
            setTimeout(function () {
                window.location.reload();
            }, 350);
        });
    });
});
