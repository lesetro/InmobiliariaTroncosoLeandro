using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContratoVenta contratoVenta)
        {
            try
            {

                var usuarioEmail = User.Identity?.Name;
                if (usuarioEmail == null)
                {
                    return RedirectToAction("Login", "Usuarios");
                }

                var usuario = await _repositorioUsuario.GetByEmailAsync(usuarioEmail);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }


                contratoVenta.IdUsuarioCreador = usuario.IdUsuario;
                Console.WriteLine($" USUARIO CREADOR ASIGNADO: {usuario.NombreCompleto} (ID: {usuario.IdUsuario})");


                ModelState.Remove("IdUsuarioCreador");

                Console.WriteLine($"📥 DATOS RECIBIDOS EN POST:");
                Console.WriteLine($"   IdInmueble: {contratoVenta.IdInmueble}");
                Console.WriteLine($"   IdVendedor: {contratoVenta.IdVendedor}");
                Console.WriteLine($"   IdComprador: {contratoVenta.IdComprador}");
                Console.WriteLine($"   IdUsuarioCreador: {contratoVenta.IdUsuarioCreador}");
                Console.WriteLine($"   PrecioTotal: {contratoVenta.PrecioTotal}");
                Console.WriteLine($"   MontoSeña: {contratoVenta.MontoSeña}");
                Console.WriteLine($"   FechaInicio: {contratoVenta.FechaInicio}");


                Console.WriteLine($" MODEL STATE VALID: {ModelState.IsValid}");

                if (!ModelState.IsValid)
                {
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;
                        if (errors.Count > 0)
                        {
                            Console.WriteLine($" ERROR en {key}:");
                            foreach (var error in errors)
                            {
                                Console.WriteLine($"   - {error.ErrorMessage}");
                            }
                        }
                    }

                    return View(contratoVenta);
                }

                // Resto de las validaciones (comprador, vendedor, inmueble)...
                var comprador = await _repositorioUsuario.GetByIdAsync(contratoVenta.IdComprador);
                if (comprador == null)
                {
                    Console.WriteLine($" COMPRADOR NO ENCONTRADO: ID {contratoVenta.IdComprador}");
                    ModelState.AddModelError("IdComprador", "El comprador seleccionado no existe en el sistema");
                    return View(contratoVenta);
                }

                var vendedor = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(contratoVenta.IdVendedor);
                if (vendedor == null)
                {
                    Console.WriteLine($" VENDEDOR NO ENCONTRADO: ID {contratoVenta.IdVendedor}");
                    ModelState.AddModelError("IdVendedor", "El vendedor seleccionado no existe en el sistema");
                    return View(contratoVenta);
                }

                var inmueble = await _repositorioInmueble.ObtenerInmueblePorIdAsync(contratoVenta.IdInmueble);
                if (inmueble == null)
                {
                    Console.WriteLine($" INMUEBLE NO ENCONTRADO: ID {contratoVenta.IdInmueble}");
                    ModelState.AddModelError("IdInmueble", "El inmueble seleccionado no existe en el sistema");
                    return View(contratoVenta);
                }

                // Verificar disponibilidad
                var inmuebleDisponible = await _repositorioInmueble.VerificarDisponibilidadParaVenta(contratoVenta.IdInmueble);
                if (!inmuebleDisponible)
                {
                    Console.WriteLine($" INMUEBLE NO DISPONIBLE: ID {contratoVenta.IdInmueble}");
                    ModelState.AddModelError("IdInmueble", "El inmueble seleccionado no está disponible para venta");
                    return View(contratoVenta);
                }

                // Completar datos del contrato
                contratoVenta.FechaCreacion = DateTime.Now;
                contratoVenta.FechaModificacion = DateTime.Now;

                // Si hay seña, actualizar estado
                if (contratoVenta.MontoSeña > 0)
                {
                    contratoVenta.MontoPagado = contratoVenta.MontoSeña;
                    contratoVenta.Estado = "seña_pagada";
                }

                Console.WriteLine($" CREANDO CONTRATO CON DATOS:");
                Console.WriteLine($"   Inmueble: {inmueble.Direccion} (ID: {contratoVenta.IdInmueble})");
                Console.WriteLine($"   Vendedor: {vendedor.Usuario?.NombreCompleto} (ID: {contratoVenta.IdVendedor})");
                Console.WriteLine($"   Comprador: {comprador.NombreCompleto} (ID: {contratoVenta.IdComprador})");
                Console.WriteLine($"   Usuario Creador: {usuario.NombreCompleto} (ID: {contratoVenta.IdUsuarioCreador})");
                Console.WriteLine($"   Precio: {contratoVenta.PrecioTotal}");
                Console.WriteLine($"   Seña: {contratoVenta.MontoSeña}");

                await _repositorioContratoVenta.CrearAsync(contratoVenta);

                Console.WriteLine($" CONTRATO CREADO EXITOSAMENTE - ID: {contratoVenta.IdContratoVenta}");

                TempData["SuccessMessage"] = "Contrato de venta creado exitosamente";
                return RedirectToAction("Details", new { id = contratoVenta.IdContratoVenta });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR CRÍTICO: {ex.Message}");
                Console.WriteLine($" STACK TRACE: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Error al crear el contrato de venta: {ex.Message}";
                return View(contratoVenta);
            }

        }
        // GET: ContratoVenta/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID de contrato inválido";
                    return RedirectToAction("Index", "Contratos");
                }

                var contrato = await _repositorioContratoVenta.ObtenerCompletoPorIdAsync(id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar que no esté ya cancelado
                if (contrato.Estado == "cancelada")
                {
                    TempData["ErrorMessage"] = "El contrato ya está cancelado";
                    return RedirectToAction("Details", new { id });
                }

                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // POST: ContratoVenta/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Delete(int id, string motivoCancelacion)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID de contrato inválido";
                    return RedirectToAction("Index", "Contratos");
                }

                var contrato = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar que no esté ya cancelado
                if (contrato.Estado == "cancelada")
                {
                    TempData["ErrorMessage"] = "El contrato ya está cancelado";
                    return RedirectToAction("Details", new { id });
                }

                // Obtener usuario actual para el cancelador
                var usuarioEmail = User.Identity?.Name;
                if (usuarioEmail == null)
                {
                    TempData["ErrorMessage"] = "Usuario no autenticado";
                    return RedirectToAction("Index", "Contratos");
                }

                var usuario = await _repositorioUsuario.GetByEmailAsync(usuarioEmail);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Cancelar contrato con motivo
                contrato.Cancelar(usuario.IdUsuario, motivoCancelacion);
                contrato.FechaModificacion = DateTime.Now;

                await _repositorioContratoVenta.ActualizarAsync(contrato);

                TempData["SuccessMessage"] = "Contrato cancelado exitosamente";
                return RedirectToAction("Index", "Contratos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cancelar contrato: {ex.Message}";
                return RedirectToAction("Delete", new { id });
            }
        }

        // MÉTODO PARA BUSCAR INMUEBLES 
        [HttpGet]
        public async Task<IActionResult> BuscarInmuebles(string termino, int limite = 10)
        {
            try
            {

                var inmuebles = await _repositorioInmueble.BuscarParaVentaAsync(termino, limite);

                var resultados = inmuebles.Select(i => new
                {
                    Id = i.IdInmueble,
                    Texto = $"{i.Direccion ?? "Sin dirección"} - ${i.Precio:N2}",
                    TextoCompleto = $"{i.Direccion ?? "Sin dirección"} | ${i.Precio:N2} | {i.Estado ?? "N/D"}",
                    Precio = i.Precio,
                    Estado = i.Estado
                }).ToList();

                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error al buscar inmuebles" });
            }
        }

        // MÉTODO PARA BUSCAR VENDEDORES 
        [HttpGet]
        public async Task<IActionResult> BuscarVendedores(string termino, int limite = 10)
        {
            try
            {

                var propietarios = await _repositorioPropietario.ObtenerTodosAsync();
                var listaFiltrada = propietarios.ToList();

                // Filtrar localmente por término
                if (!string.IsNullOrEmpty(termino))
                {
                    listaFiltrada = listaFiltrada.Where(p =>
                        (p.Usuario?.Nombre?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true) ||
                        (p.Usuario?.Apellido?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true) ||
                        (p.Usuario?.Dni?.Contains(termino) == true)
                    ).Take(limite).ToList();
                }
                else
                {
                    listaFiltrada = listaFiltrada.Take(limite).ToList();
                }

                var resultados = listaFiltrada.Select(p => new
                {
                    Id = p.IdPropietario,
                    Texto = $"{p.Usuario?.Nombre} {p.Usuario?.Apellido}".Trim() ?? "Sin nombre",
                    TextoCompleto = $"{p.Usuario?.Nombre} {p.Usuario?.Apellido} | DNI: {p.Usuario?.Dni ?? "N/D"}"
                }).ToList();

                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error al buscar vendedores" });
            }
        }

        // MÉTODO PARA BUSCAR COMPRADORES - USA EL QUE SÍ TIENES
        [HttpGet]
        public async Task<IActionResult> BuscarCompradores(string termino, int limite = 10)
        {
            try
            {

                var usuarios = await _repositorioUsuario.GetAllAsync();
                var listaFiltrada = usuarios.ToList();

                // Filtrar localmente por término
                if (!string.IsNullOrEmpty(termino))
                {
                    listaFiltrada = listaFiltrada.Where(u =>
                        (u.Nombre?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true) ||
                        (u.Apellido?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true) ||
                        (u.Dni?.Contains(termino) == true)
                    ).Take(limite).ToList();
                }
                else
                {
                    listaFiltrada = listaFiltrada.Take(limite).ToList();
                }

                var resultados = listaFiltrada.Select(u => new
                {
                    Id = u.IdUsuario,
                    Texto = u.NombreCompleto ?? "Sin nombre",
                    TextoCompleto = $"{u.NombreCompleto} | DNI: {u.Dni ?? "N/D"}"
                }).ToList();

                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error al buscar compradores" });
            }
        }

        // MÉTODO PARA OBTENER INMUEBLES DE UN VENDEDOR 
        [HttpGet]
        public async Task<IActionResult> ObtenerInmueblesPorVendedor(int vendedorId, int limite = 15)
        {
            try
            {

                var inmuebles = await _repositorioInmueble.ObtenerPorPropietarioAsync(vendedorId);

                // Filtrar solo los disponibles para venta
                var inmueblesDisponibles = inmuebles.Where(i =>
                    (i.Estado == "disponible" || i.Estado == "venta") && i.Precio > 0
                ).Take(limite);

                var resultados = inmueblesDisponibles.Select(i => new
                {
                    Id = i.IdInmueble,
                    Texto = $"{i.Direccion ?? "Sin dirección"} - ${i.Precio:N2}",
                    TextoCompleto = $"{i.Direccion ?? "Sin dirección"} | ${i.Precio:N2}",
                    Precio = i.Precio
                }).ToList();

                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error al obtener inmuebles del vendedor" });
            }
        }

        // MÉTODO PARA OBTENER VENDEDOR DE UN INMUEBLE
        [HttpGet]
        public async Task<IActionResult> ObtenerVendedorDeInmueble(int inmuebleId)
        {
            try
            {
                if (inmuebleId <= 0)
                {
                    return Json(new { success = false, error = "ID de inmueble inválido" });
                }

                // Obtener el inmueble para saber el propietario
                var inmueble = await _repositorioInmueble.ObtenerInmueblePorIdAsync(inmuebleId);

                if (inmueble == null)
                {
                    return Json(new { success = false, error = "Inmueble no encontrado" });
                }

                // Obtener el propietario (vendedor)
                var propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(inmueble.IdPropietario);

                if (propietario == null)
                {
                    return Json(new { success = false, error = "Propietario no encontrado" });
                }

                var vendedorResult = new
                {
                    Id = propietario.IdPropietario,
                    Texto = $"{propietario.Usuario?.Nombre} {propietario.Usuario?.Apellido}".Trim() ?? "Sin nombre",
                    TextoCompleto = $"{propietario.Usuario?.Nombre} {propietario.Usuario?.Apellido} | DNI: {propietario.Usuario?.Dni ?? "N/D"}"
                };

                return Json(new { success = true, data = vendedorResult });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error al obtener vendedor" });
            }
        }

        // GET: ContratoVenta/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID de contrato inválido";
                    return RedirectToAction("Index", "Contratos");
                }

                var contrato = await _repositorioContratoVenta.ObtenerCompletoPorIdAsync(id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Calcular montos adicionales para la vista
                ViewBag.SaldoPendiente = contrato.SaldoPendiente;
                ViewBag.PorcentajeCompletado = contrato.PorcentajePagado;
                ViewBag.EstaCompleto = contrato.EstaCompleta;

                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }
        // GET: ContratoVenta/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                Console.WriteLine($"=== INICIANDO EDIT - ID: {id} ===");

                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID de contrato inválido";
                    return RedirectToAction("Index", "Contratos");
                }

                Console.WriteLine($"📋 Buscando contrato con ID: {id}");

                var contrato = await _repositorioContratoVenta.ObtenerCompletoPorIdAsync(id);

                if (contrato == null)
                {
                    Console.WriteLine($"❌ CONTRATO NO ENCONTRADO - ID: {id}");
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                Console.WriteLine($"✅ CONTRATO ENCONTRADO:");
                Console.WriteLine($"   ID: {contrato.IdContratoVenta}");
                Console.WriteLine($"   Inmueble: {contrato.Inmueble?.Direccion}");
                Console.WriteLine($"   Vendedor: {contrato.Vendedor?.Usuario?.NombreCompleto}");
                Console.WriteLine($"   Comprador: {contrato.Comprador?.NombreCompleto}");
                Console.WriteLine($"   Estado: {contrato.Estado}");
                Console.WriteLine($"   Precio: {contrato.PrecioTotal}");

                // Validar que el contrato no esté cancelado o escriturado
                if (contrato.Estado == "cancelada" || contrato.Estado == "escriturada")
                {
                    Console.WriteLine($"🚫 CONTRATO NO EDITABLE - Estado: {contrato.Estado}");
                    TempData["ErrorMessage"] = "No se puede editar un contrato cancelado o escriturado";
                    return RedirectToAction("Details", new { id });
                }

                Console.WriteLine($"✅ CONTRATO ES EDITABLE");

                // Cargar datos para los dropdowns en ViewBag
                await CargarDatosParaFormulario(contrato);

                Console.WriteLine($"📦 DATOS CARGADOS - Redirigiendo a vista Edit");
                return View(contrato);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ERROR CRÍTICO: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }
        private async Task CargarDatosParaFormulario(ContratoVenta contrato = null)
        {
            // Cargar datos para mostrar información en el formulario
            if (contrato?.Inmueble != null)
            {
                ViewBag.DireccionInmueble = contrato.Inmueble.Direccion;
                ViewBag.PrecioInmueble = contrato.Inmueble.Precio;
            }

            if (contrato?.Vendedor?.Usuario != null)
            {
                ViewBag.NombreVendedor = $"{contrato.Vendedor.Usuario.Nombre} {contrato.Vendedor.Usuario.Apellido}";
            }

            if (contrato?.Comprador != null)
            {
                ViewBag.NombreComprador = contrato.Comprador.NombreCompleto;
            }
        }
        // POST: ContratoVenta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContratoVenta contratoVenta)
        {
            if (id != contratoVenta.IdContratoVenta)
            {
                TempData["ErrorMessage"] = "ID de contrato no coincide";
                return RedirectToAction("Index", "Contratos");
            }

            try
            {
                // Validar que el contrato exista y obtener datos completos
                var contratoExistente = await _repositorioContratoVenta.ObtenerCompletoPorIdAsync(id);
                if (contratoExistente == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar que no esté cancelado o escriturado
                if (contratoExistente.Estado == "cancelada" || contratoExistente.Estado == "escriturada")
                {
                    TempData["ErrorMessage"] = "No se puede editar un contrato cancelado o escriturado";
                    return RedirectToAction("Details", new { id });
                }

                // Validaciones de negocio
                if (contratoVenta.PrecioTotal <= 0)
                {
                    ModelState.AddModelError("PrecioTotal", "El precio total debe ser mayor a 0");
                }

                if (contratoVenta.MontoSeña > contratoVenta.PrecioTotal)
                {
                    ModelState.AddModelError("MontoSeña", "La seña no puede ser mayor al precio total");
                }

                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario(contratoExistente);
                    return View(contratoVenta);
                }

                // Preservar datos que no se editan
                contratoVenta.FechaCreacion = contratoExistente.FechaCreacion;
                contratoVenta.IdUsuarioCreador = contratoExistente.IdUsuarioCreador;
                contratoVenta.FechaModificacion = DateTime.Now;

                // Mantener datos de relaciones que no se editan
                contratoVenta.Inmueble = contratoExistente.Inmueble;
                contratoVenta.Vendedor = contratoExistente.Vendedor;
                contratoVenta.Comprador = contratoExistente.Comprador;
                contratoVenta.UsuarioCreador = contratoExistente.UsuarioCreador;

                // Manejar lógica de estado y montos pagados
                if (contratoVenta.MontoSeña > 0 && contratoExistente.Estado == "seña_pendiente")
                {
                    // Si se agregó seña por primera vez
                    contratoVenta.Estado = "seña_pagada";
                    contratoVenta.MontoPagado = contratoVenta.MontoSeña;
                }
                else if (contratoVenta.MontoSeña != contratoExistente.MontoSeña)
                {
                    // Si se modificó la seña, ajustar monto pagado
                    var diferenciaSeña = contratoVenta.MontoSeña - contratoExistente.MontoSeña;
                    contratoVenta.MontoPagado = Math.Max(0, contratoExistente.MontoPagado + diferenciaSeña);
                }
                else
                {
                    // Mantener estado y monto pagado existente
                    contratoVenta.Estado = contratoExistente.Estado;
                    contratoVenta.MontoPagado = contratoExistente.MontoPagado;
                }

                // Actualizar porcentaje pagado
                contratoVenta.PorcentajePagado = contratoVenta.PrecioTotal > 0
                    ? (contratoVenta.MontoPagado / contratoVenta.PrecioTotal) * 100
                    : 0;

                await _repositorioContratoVenta.ActualizarAsync(contratoVenta);

                TempData["SuccessMessage"] = "Contrato de venta actualizado exitosamente";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar contrato: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(contratoVenta);
            }
        }


        // GET: ContratoVenta/MarcarEscriturada/5
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> MarcarEscriturada(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID de contrato inválido";
                    return RedirectToAction("Index", "Contratos");
                }

                var contrato = await _repositorioContratoVenta.ObtenerCompletoPorIdAsync(id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato de venta no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar que el contrato esté listo para escriturar
                if (contrato.Estado == "escriturada")
                {
                    TempData["ErrorMessage"] = "El contrato ya está escriturado";
                    return RedirectToAction("Details", new { id });
                }

                if (contrato.Estado != "pendiente_escritura" && contrato.MontoPagado < contrato.PrecioTotal)
                {
                    TempData["ErrorMessage"] = "El contrato no está completamente pagado para escriturar";
                    return RedirectToAction("Details", new { id });
                }

                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction("Index", "Contratos");
            }
        }

        // POST: ContratoVenta/MarcarEscriturada/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> MarcarEscriturada(int id, DateTime fechaEscritura)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID de contrato inválido";
                    return RedirectToAction("Index", "Contratos");
                }

                var contrato = await _repositorioContratoVenta.ObtenerPorIdAsync(id);
                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction("Index", "Contratos");
                }

                // Validar fecha de escritura
                if (fechaEscritura < contrato.FechaInicio)
                {
                    TempData["ErrorMessage"] = "La fecha de escritura no puede ser anterior a la fecha de inicio del contrato";
                    return RedirectToAction("MarcarEscriturada", new { id });
                }

                // Marcar como escriturada
                contrato.MarcarEscriturada(fechaEscritura);
                contrato.FechaModificacion = DateTime.Now;

                await _repositorioContratoVenta.ActualizarAsync(contrato);

                TempData["SuccessMessage"] = "Contrato marcado como escriturado exitosamente";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al marcar como escriturado: {ex.Message}";
                return RedirectToAction("MarcarEscriturada", new { id });
            }
        }

    }
}