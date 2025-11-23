themeToggle.onclick = () => {
  document.body.classList.toggle("light");
  if (document.body.classList.contains("light")) {
    themeToggle.textContent = "☀️ Light Mode";
  } else {
    themeToggle.textContent = "🌙 Dark Mode";
  }
};