using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize]
    public class ContratoVentaController : Controller
    {
        private readonly IRepositorioContratoVenta _repositorioContratoVenta;
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioPropietario _repositorioPropietario;
        private readonly IRepositorioUsuario _repositorioUsuario;
        

        public ContratoVentaController(
            IRepositorioContratoVenta repositorioContratoVenta,
            IRepositorioInmueble repositorioInmueble,
            IRepositorioPropietario repositorioPropietario,
            IRepositorioUsuario repositorioUsuario)
        {
            _repositorioContratoVenta = repositorioContratoVenta;
            _repositorioInmueble = repositorioInmueble;
            _repositorioPropietario = repositorioPropietario;
            _repositorioUsuario = repositorioUsuario;
        }

        // GET: ContratoVenta/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarViewData();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // POST: ContratoVenta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContratoVenta contratoVenta)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarViewData();
                    return View(contratoVenta);
                }

                // Validaciones adicionales
                var errores = contratoVenta.ValidarContrato();
                if (errores.Any())
                {
                    foreach (var error in errores)
                    {
                        ModelState.AddModelError("", error);
                    }
                    await CargarViewData();
                    return View(contratoVenta);
                }

                // Verificar que el inmueble esté disponible
                var inmuebleDisponible = await _repositorioInmueble.VerificarDisponibilidadParaVenta(contratoVenta.IdInmueble);
                if (!inmuebleDisponible)
                {
                    ModelState.AddModelError("IdInmueble", "El inmueble seleccionado no está disponible para venta");
                    await CargarViewData();
                    return View(contratoVenta);
                }

                // Obtener ID del usuario logueado
                var usuarioEmail = User.Identity.Name;
                var usuario = await _repositorioUsuario.ObtenerPorEmailAsync(usuarioEmail);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                contratoVenta.IdUsuarioCreador = usuario.IdUsuario;
                contratoVenta.FechaCreacion = DateTime.Now;
                contratoVenta.FechaModificacion = DateTime.Now;

                // Si hay seña, actualizar estado
                if (contratoVenta.MontoSeña > 0)
                {
                    contratoVenta.MontoPagado = contratoVenta.MontoSeña;
                    contratoVenta.Estado = "seña_pagada";
                    contratoVenta.ActualizarMontoPagado();
                }

                await _repositorioContratoVenta.CrearAsync(contratoVenta);

                TempData["SuccessMessage"] = "Contrato de venta creado exitosamente";
                return RedirectToAction("Index", "Contratos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear el contrato de venta: {ex.Message}";
                await CargarViewData();
                return View(contratoVenta);
            }
        }

        // GET: ContratoVenta/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var contratoVenta = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contratoVenta == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar que no esté cancelado o escriturado
                if (contratoVenta.Estado == "cancelada" || contratoVenta.Estado == "escriturada")
                {
                    TempData["ErrorMessage"] = $"No se puede editar un contrato en estado: {contratoVenta.EstadoDescripcion}";
                    return RedirectToAction("Index", "Contratos");
                }

                await CargarViewData(contratoVenta);
                return View(contratoVenta);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el contrato: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // POST: ContratoVenta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContratoVenta contratoVenta)
        {
            try
            {
                if (id != contratoVenta.IdContratoVenta)
                {
                    TempData["ErrorMessage"] = "ID del contrato no coincide";
                    return RedirectToAction("Index", "Contratos");
                }

                if (!ModelState.IsValid)
                {
                    await CargarViewData(contratoVenta);
                    return View(contratoVenta);
                }

                // Obtener contrato existente
                var contratoExistente = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contratoExistente == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar que no esté cancelado o escriturado
                if (contratoExistente.Estado == "cancelada" || contratoExistente.Estado == "escriturada")
                {
                    TempData["ErrorMessage"] = $"No se puede editar un contrato en estado: {contratoExistente.EstadoDescripcion}";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validaciones adicionales
                var errores = contratoVenta.ValidarContrato();
                if (errores.Any())
                {
                    foreach (var error in errores)
                    {
                        ModelState.AddModelError("", error);
                    }
                    await CargarViewData(contratoVenta);
                    return View(contratoVenta);
                }

                // Actualizar propiedades
                contratoExistente.IdInmueble = contratoVenta.IdInmueble;
                contratoExistente.IdComprador = contratoVenta.IdComprador;
                contratoExistente.IdVendedor = contratoVenta.IdVendedor;
                contratoExistente.FechaInicio = contratoVenta.FechaInicio;
                contratoExistente.FechaEscrituracion = contratoVenta.FechaEscrituracion;
                contratoExistente.PrecioTotal = contratoVenta.PrecioTotal;
                contratoExistente.MontoSeña = contratoVenta.MontoSeña;
                contratoExistente.MontoAnticipos = contratoVenta.MontoAnticipos;
                contratoExistente.Observaciones = contratoVenta.Observaciones;
                contratoExistente.FechaModificacion = DateTime.Now;

                // Actualizar montos y estado
                contratoExistente.ActualizarMontoPagado();

                await _repositorioContratoVenta.ActualizarAsync(contratoExistente);

                TempData["SuccessMessage"] = "Contrato de venta actualizado exitosamente";
                return RedirectToAction("Index", "Contratos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar el contrato: {ex.Message}";
                await CargarViewData(contratoVenta);
                return View(contratoVenta);
            }
        }

        // GET: ContratoVenta/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var contratoVenta = await _repositorioContratoVenta.ObtenerCompletoPorIdAsync(id);
                if (contratoVenta == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                return View(contratoVenta);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los detalles: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // GET: ContratoVenta/Cancel/5
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var contratoVenta = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contratoVenta == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                if (contratoVenta.Estado == "cancelada")
                {
                    TempData["WarningMessage"] = "El contrato ya está cancelado";
                    return RedirectToAction("Index", "Contratos");
                }

                if (contratoVenta.Estado == "escriturada")
                {
                    TempData["ErrorMessage"] = "No se puede cancelar un contrato ya escriturado";
                    return RedirectToAction("Index", "Contratos");
                }

                return View(contratoVenta);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el formulario de cancelación: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // POST: ContratoVenta/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string motivoCancelacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivoCancelacion))
                {
                    TempData["ErrorMessage"] = "Debe proporcionar un motivo de cancelación";
                    return RedirectToAction("Cancel", new { id });
                }

                var contratoVenta = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contratoVenta == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Obtener ID del usuario logueado
                var usuarioEmail = User.Identity.Name;
                var usuario = await _repositorioUsuario.ObtenerPorEmailAsync(usuarioEmail);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Cancelar contrato
                contratoVenta.Cancelar(usuario.IdUsuario, motivoCancelacion);
                contratoVenta.FechaModificacion = DateTime.Now;

                await _repositorioContratoVenta.ActualizarAsync(contratoVenta);

                TempData["SuccessMessage"] = "Contrato de venta cancelado exitosamente";
                return RedirectToAction("Index", "Contratos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cancelar el contrato: {ex.Message}";
                return RedirectToAction("Cancel", new { id });
            }
        }

        // GET: ContratoVenta/Escriturar/5
        public async Task<IActionResult> Escriturar(int id)
        {
            try
            {
                var contratoVenta = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contratoVenta == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                if (contratoVenta.Estado == "escriturada")
                {
                    TempData["WarningMessage"] = "El contrato ya está escriturado";
                    return RedirectToAction("Index", "Contratos");
                }

                if (contratoVenta.Estado == "cancelada")
                {
                    TempData["ErrorMessage"] = "No se puede escriturar un contrato cancelado";
                    return RedirectToAction("Index", "Contratos");
                }

                if (!contratoVenta.EstaCompleta)
                {
                    TempData["ErrorMessage"] = "No se puede escriturar un contrato con pagos pendientes";
                    return RedirectToAction("Index", "Contratos");
                }

                return View(contratoVenta);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el formulario de escrituración: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // POST: ContratoVenta/Escriturar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Escriturar(int id, DateTime fechaEscrituracion)
        {
            try
            {
                if (fechaEscrituracion < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "La fecha de escrituración no puede ser anterior a hoy";
                    return RedirectToAction("Escriturar", new { id });
                }

                var contratoVenta = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contratoVenta == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Marcar como escriturado
                contratoVenta.MarcarEscriturada(fechaEscrituracion);
                contratoVenta.FechaModificacion = DateTime.Now;

                await _repositorioContratoVenta.ActualizarAsync(contratoVenta);

                TempData["SuccessMessage"] = "Contrato marcado como escriturado exitosamente";
                return RedirectToAction("Index", "Contratos");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Escriturar", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al procesar la escrituración: {ex.Message}";
                return RedirectToAction("Escriturar", new { id });
            }
        }

        // Método auxiliar para cargar datos en ViewData
        private async Task CargarViewData(ContratoVenta? contratoVenta = null)
        {
            try
            {
                // Cargar inmuebles disponibles para venta
                var inmuebles = await _repositorioInmueble.ObtenerDisponiblesParaVentaAsync();
                ViewData["IdInmueble"] = new SelectList(inmuebles, "IdInmueble", "Direccion", contratoVenta?.IdInmueble);

                // Cargar propietarios (vendedores)
                var propietarios = await _repositorioPropietario.ObtenerTodosAsync();
                ViewData["IdVendedor"] = new SelectList(propietarios, "IdPropietario", "Usuario.NombreCompleto", contratoVenta?.IdVendedor);

                // Cargar usuarios (compradores)
                var usuarios = await _repositorioUsuario.ObtenerTodosAsync();
                ViewData["IdComprador"] = new SelectList(usuarios, "IdUsuario", "NombreCompleto", contratoVenta?.IdComprador);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar datos del formulario: {ex.Message}", ex);
            }
        }
        
    }
}