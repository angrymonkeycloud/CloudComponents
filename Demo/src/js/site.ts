function myFunction(): void {
   
    var links = document.querySelector(".components-links");
   
    if (links.classList.contains("_clicked")) {
        links.classList.remove("_clicked");
    }
    else
    {
        links.classList.add("_clicked");
    }
}