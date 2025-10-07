using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Inmobiliaria_troncoso_leandro.Filters
{
    public class LayoutActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Se ejecuta ANTES de la acción
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Controller is Controller controller)
            {
                var user = context.HttpContext.User;
                string layout = DetermineLayout(user);
                
                // Guardar en ambos lugares para compatibilidad
                controller.ViewData["Layout"] = layout;
                context.HttpContext.Items["SelectedLayout"] = layout;
            }
        }

        private string DetermineLayout(ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated)
                return "_LayoutPublic";

            var role = user.FindFirst(ClaimTypes.Role)?.Value?.ToLower() ?? "";
            
            return role switch
            {
                "administrador" => "_LayoutAdmin",
                "empleado" => "_LayoutEmpleado", 
                "propietario" => "_LayoutPropietario",
                "inquilino" => "_LayoutInquilino",
                _ => "_LayoutPublic"
            };
        }
    }
}