# Sistema de Gestión Inmobiliaria

Sistema web completo para gestión de inmobiliaria desarrollado en **ASP.NET Core MVC** con arquitectura robusta y funcionalidades integrales para el manejo de propiedades, contratos, pagos y más.

## Usuarios y contraseñas

Administrador 

Correo=  test2@inmobiliaria.com 
Contraseña = Test1234

Empleado 

Correo=  test4@inmobiliaria04.com
Contraseña = PasswordTemporal123

Propietario

Correo=  andrea@gmail.com
Contraseña = PasswordTemporal123
Inquilino

Correo=  leandrosebastianTronco@gmail.com
Contraseña = PasswordTemporal123

##  Inicio Rápido

###  Configuración Inicial de Administrador

**IMPORTANTE:** Para usar el sistema necesitas un usuario administrador. Si es la primera vez que ejecutas la aplicación:

1. **Si NO existe administrador:** El sistema detectará automáticamente esta situación y te presentará un **setup inicial** que solicita:
   - Nombre y apellido del administrador
   - Email (será tu usuario de login)
   - DNI y teléfono
   - Contraseña segura

2. **Si YA existe administrador:** Ingresa directamente con tus credenciales en `/Home/Login`

## 🛠️ Tecnologías Empleadas

- **Framework:** ASP.NET Core MVC
- **Base de Datos:** MySQL
- **ORM:** ADO.NET 
- **Frontend:** Bootstrap 5, HTML5, CSS3, JavaScript
- **Autenticación:** Cookie Authentication con políticas de autorización
- **Seguridad:** BCrypt para hash de contraseñas
- **Gestión de Archivos:** IFormFile para subida de imágenes

##  Arquitectura del Proyecto

```
📁 Inmobiliaria_troncoso_leandro/
├── 📁 Controllers/          # Controladores MVC
├── 📁 Models/              # Modelos de datos
├── 📁 Data/
│   ├── 📁 Interfaces/      # Interfaces de repositorios
│   └── 📁 Repositorios/    # Implementación de repositorios
├── 📁 Views/               # Vistas Razor
├── 📁 wwwroot/
│   ├── 📁 css/            # Estilos CSS
│   ├── 📁 js/             # Scripts JavaScript
│   └── 📁 images/         # Imágenes del sistema
└── 📁 Services/           # Servicios de negocio
```

##  Funcionalidades Principales

###  Gestión de Usuarios
- **4 Roles:** Administrador, Empleado, Propietario, Inquilino
- Sistema de autenticación con cookies
- Políticas de autorización por rol
- Gestión completa de perfiles con avatares

###  Gestión de Inmuebles
- **CRUD completo** de propiedades
- **Galería de imágenes** con portada personalizable
- **Tipos de inmueble** configurables (Casa, Departamento, Local, Oficina)
- **Geolocalización** con coordenadas
- **Estados:** Disponible, Alquilado, Vendido, Mantenimiento
- **Manejo de archivos:** Imagenes, Cargar comprobantes de pago

###  Gestión de Contratos
- **Contratos de alquiler** con fechas y montos
- **Vinculación** inmueble-propietario-inquilino
- **Estados:** Vigente, Vencido, Finalizado
- **Cálculo automático** de vencimientos

###  Sistema de Pagos
- **Pagos de alquiler** vinculados a contratos
- **Pagos de venta** para compra de inmuebles
- **Cálculo de mora** automático
- **Comprobantes** digitales (PDF, imágenes)
- **Reportes y estadísticas**

###  Funcionalidades Adicionales
- **Dashboard** con métricas en tiempo real
- **Búsqueda avanzada** con filtros múltiples
- **Paginación** optimizada para grandes volúmenes
- **Exportación** de datos a CSV


## Instalación y Configuración

### Prerrequisitos
- .NET 6.0 o superior
- MySQL Server
- Visual Studio 2022 o VS Code

### Pasos de Instalación

1. **Clonar el repositorio**
```bash
git clone [URL-del-repositorio]
cd Inmobiliaria_troncoso_leandro
```

2. **Configurar la base de datos**
   // tengo que hacer un database, para que se ejecute sin datos.

3. **Configurar connection string**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=inmobiliaria_db;Uid=tu_usuario;Pwd=;"
  }
}
```

4. **Ejecutar la aplicación**
```bash
dotnet run
```

5. **Acceder al sistema**
   - Abrir navegador en `https://localhost:5222`
   - Completar setup inicial si es primera ejecución
   - ¡Listo para usar! 🎉

## 🔐 Sistema de Roles y Permisos

| Rol | Permisos |
|-----|----------|
| **Administrador** | Acceso total al sistema, gestión de usuarios |
| **Empleado** | Gestión de inmuebles, contratos y pagos |
| **Propietario** | Ver sus inmuebles y contratos |
| **Inquilino** | Ver sus contratos y pagos |

## 📸 Gestión de Imágenes

- **Portadas** de inmuebles con redimensionado automático
- **Galerías** completas por propiedad
- **Validación** de formatos (JPG, PNG, GIF)
- **Almacenamiento** optimizado en estructura de carpetas

## 🔍 Características Técnicas

- **Patrón Repository** para acceso a datos
- **Inyección de dependencias** nativa de .NET Core
- **Transacciones** para operaciones críticas
- **Validaciones** tanto client-side como server-side
- **Logs** detallados para debugging
- **Manejo de errores** robusto con try-catch



**Desarrollado por:** Leandro Troncoso  
**Tecnología:** ASP.NET Core MVC + MySQL  
**Licencia:** [Especificar licencia]
**Laboratirio Programacion 2 , Profesor :** Mariano Luzza

---

