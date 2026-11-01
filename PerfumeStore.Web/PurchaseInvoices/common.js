const API = window.API_ENDPOINTS.purchaseInvoices;

document.addEventListener("DOMContentLoaded", () => {
  const searchBox = $("searchInput");

  let timer = null;

  const triggerLoad = () => {
    clearTimeout(timer);
    timer = setTimeout(() => {
      if (typeof loadList === "function") {
        loadList(searchBox?.value || "");
      }
    }, 350);
  };

  if (searchBox) {
    searchBox.addEventListener("input", triggerLoad);
  }


});