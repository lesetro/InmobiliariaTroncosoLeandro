// site.js

// Efecto de texto sobre imagen (typewriter)
document.addEventListener("DOMContentLoaded", function () {
  const overlayText = document.querySelector(".overlay-text");
  if (overlayText) {
    const text =
      "En cada cerradura, una promesa; en cada llave, la felicidad empieza, Encuentra tu hogar en CIMA. ";
    let i = 0;
    const speed = 100; // Velocidad de escritura en milisegundos

    function typeWriter() {
      if (i < text.length) {
        overlayText.innerHTML += text.charAt(i);
        i++;
        setTimeout(typeWriter, speed);
      } 
    }

    // Inicia la animación
    typeWriter();
  }
});

// Efectos adicionales (ej. smooth scroll para anclas)
document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
  anchor.addEventListener("click", function (e) {
    e.preventDefault();
    document.querySelector(this.getAttribute("href")).scrollIntoView({
      behavior: "smooth",
    });
  });
});
