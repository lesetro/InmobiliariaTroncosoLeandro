using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using System.Security.Claims;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "EmpleadoOSuperior")] // Empleados y superiores pueden acceder
    public class EmpleadoController : Controller
    {
        private readonly IRepositorioEmpleado _repositorioEmpleado;
        private readonly IWebHostEnvironment _environment;

        public EmpleadoController(IRepositorioEmpleado repositorioEmpleado, IWebHostEnvironment environment)
        {
            _repositorioEmpleado = repositorioEmpleado;
            _environment = environment;
        }

        // GET: Empleado/Index - Dashboard del empleado
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Admin");
        }


    }
}