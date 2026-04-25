(function () {
    function toggleSidebar() {
        var sidebar = document.getElementById("sidebar");
        if (sidebar) {
            sidebar.classList.toggle("is-open");
        }
    }

    function closeSidebarOnOutsideClick(event) {
        var sidebar = document.getElementById("sidebar");
        var toggle = document.querySelector(".sidebar-toggle");

        if (!sidebar || !toggle) {
            return;
        }

        if (window.innerWidth <= 1024 && sidebar.classList.contains("is-open")) {
            if (!sidebar.contains(event.target) && !toggle.contains(event.target)) {
                sidebar.classList.remove("is-open");
            }
        }
    }

    function setupPasswordToggles() {
        var toggles = document.querySelectorAll("[data-password-toggle='true']");
        for (var i = 0; i < toggles.length; i++) {
            toggles[i].addEventListener("click", function () {
                var targetId = this.getAttribute("data-target-id");
                var input = targetId ? document.getElementById(targetId) : null;
                if (!input) {
                    return;
                }

                var showing = input.getAttribute("type") === "text";
                input.setAttribute("type", showing ? "password" : "text");
                this.classList.toggle("is-visible", !showing);
                this.setAttribute("aria-label", showing ? "Show password" : "Hide password");
                this.setAttribute("title", showing ? "Show password" : "Hide password");
            });
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        var toggles = document.querySelectorAll("[data-sidebar-toggle]");
        var overlay = document.getElementById("sidebarOverlay");

        for (var i = 0; i < toggles.length; i++) {
            toggles[i].addEventListener("click", toggleSidebar);
        }

        if (overlay) {
            overlay.addEventListener("click", toggleSidebar);
        }

        setupPasswordToggles();
        document.addEventListener("click", closeSidebarOnOutsideClick);
    });
})();
