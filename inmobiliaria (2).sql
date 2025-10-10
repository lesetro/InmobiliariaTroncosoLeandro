-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1
-- Tiempo de generación: 10-10-2025 a las 21:14:00
-- Versión del servidor: 10.4.32-MariaDB
-- Versión de PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de datos: `inmobiliaria`
--

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `contacto`
--

CREATE TABLE `contacto` (
  `id_contacto` int(11) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `email` varchar(150) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `asunto` varchar(200) DEFAULT NULL,
  `mensaje` text NOT NULL,
  `fecha_contacto` datetime DEFAULT current_timestamp(),
  `estado` enum('pendiente','respondido','cerrado') DEFAULT 'pendiente',
  `id_inmueble` int(11) DEFAULT NULL,
  `ip_origen` varchar(45) DEFAULT NULL,
  `user_agent` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `contrato`
--

CREATE TABLE `contrato` (
  `id_contrato` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `id_inquilino` int(11) NOT NULL,
  `id_propietario` int(11) NOT NULL DEFAULT 1,
  `fecha_inicio` date NOT NULL,
  `fecha_fin` date NOT NULL,
  `fecha_fin_anticipada` date DEFAULT NULL,
  `monto_mensual` decimal(12,2) NOT NULL,
  `estado` enum('vigente','finalizado','finalizado_anticipado') DEFAULT 'vigente',
  `multa_aplicada` decimal(12,2) DEFAULT 0.00,
  `id_usuario_creador` int(11) NOT NULL,
  `id_usuario_terminador` int(11) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `fecha_modificacion` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `tipo_contrato` enum('alquiler','venta','comodato','otros') NOT NULL DEFAULT 'alquiler'
) ;

--
-- Volcado de datos para la tabla `contrato`
--

INSERT INTO `contrato` (`id_contrato`, `id_inmueble`, `id_inquilino`, `id_propietario`, `fecha_inicio`, `fecha_fin`, `fecha_fin_anticipada`, `monto_mensual`, `estado`, `multa_aplicada`, `id_usuario_creador`, `id_usuario_terminador`, `fecha_creacion`, `fecha_modificacion`, `tipo_contrato`) VALUES
(2, 2, 2, 2, '2024-02-01', '2024-11-30', NULL, 80000.00, 'finalizado', 0.00, 2, NULL, '2025-08-22 19:19:05', '2025-09-16 12:09:19', 'alquiler'),
(4, 5, 1, 5, '0001-01-01', '0001-01-25', NULL, 1000000.00, 'vigente', 0.00, 6, NULL, '2025-09-05 12:08:46', '2025-09-16 12:09:19', 'alquiler'),
(5, 8, 24, 22, '2024-01-15', '2026-01-14', NULL, 95000.00, 'vigente', 0.00, 34, NULL, '2025-09-05 14:38:02', '2025-09-16 12:09:19', 'alquiler'),
(6, 8, 55, 22, '2024-01-15', '2026-01-14', NULL, 95000.00, 'vigente', 0.00, 34, NULL, '2025-09-05 14:44:48', '2025-09-16 12:09:19', 'alquiler'),
(7, 29, 1, 54, '2025-09-25', '2025-10-30', NULL, 1000.00, 'vigente', 0.00, 49, NULL, '2025-09-11 15:31:52', '2025-09-16 12:09:19', 'alquiler'),
(8, 1, 2, 1, '2025-09-12', '2025-09-30', NULL, 1000.00, 'vigente', 0.00, 34, NULL, '2025-09-11 15:34:24', '2025-09-11 15:34:24', 'alquiler'),
(9, 11, 2, 19, '2025-09-14', '2025-09-30', '2025-10-10', 1000.06, 'vigente', 0.03, 3, NULL, '2025-09-13 12:52:01', '2025-09-16 12:09:19', 'alquiler'),
(18, 29, 12, 54, '2025-11-14', '2026-05-15', NULL, 1000.01, 'vigente', 0.00, 286, NULL, '2025-09-13 22:39:42', '2025-09-16 12:09:19', 'alquiler'),
(19, 11, 43, 19, '2025-12-30', '2026-10-28', NULL, 1000.02, 'vigente', 0.00, 36, NULL, '2025-09-13 22:40:34', '2025-09-16 12:09:19', 'alquiler'),
(20, 33, 12, 50, '2026-05-20', '2027-07-28', NULL, 1000.03, 'vigente', 0.00, 324, NULL, '2025-09-13 22:41:46', '2025-09-16 12:09:19', 'alquiler'),
(21, 17, 10, 19, '2026-02-25', '2026-11-26', '2025-09-23', 1000.04, 'finalizado', 33333333.00, 286, 1, '2025-09-14 19:11:09', '2025-10-08 13:26:03', 'alquiler'),
(23, 75, 71, 71, '2025-10-11', '2026-06-30', NULL, 4820000.00, 'vigente', 0.00, 35, NULL, '2025-10-10 08:52:11', '2025-10-10 08:52:11', 'alquiler'),
(24, 78, 72, 74, '2025-10-11', '2026-01-12', NULL, 5000000.00, 'vigente', 0.00, 35, NULL, '2025-10-10 18:12:25', '2025-10-10 18:12:25', 'alquiler');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `contrato_venta`
--

CREATE TABLE `contrato_venta` (
  `id_contrato_venta` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `id_comprador` int(11) NOT NULL,
  `id_vendedor` int(11) NOT NULL,
  `fecha_inicio` date NOT NULL DEFAULT curdate(),
  `fecha_escrituracion` date DEFAULT NULL,
  `fecha_cancelacion` date DEFAULT NULL,
  `precio_total` decimal(15,2) NOT NULL,
  `monto_seña` decimal(15,2) DEFAULT 0.00,
  `monto_anticipos` decimal(15,2) DEFAULT 0.00,
  `monto_pagado` decimal(15,2) DEFAULT 0.00,
  `estado` varchar(30) NOT NULL DEFAULT 'seña_pendiente',
  `porcentaje_pagado` decimal(5,2) DEFAULT 0.00,
  `id_usuario_creador` int(11) NOT NULL,
  `id_usuario_cancelador` int(11) DEFAULT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `fecha_modificacion` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `observaciones` varchar(500) DEFAULT NULL,
  `motivo_cancelacion` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `contrato_venta`
--

INSERT INTO `contrato_venta` (`id_contrato_venta`, `id_inmueble`, `id_comprador`, `id_vendedor`, `fecha_inicio`, `fecha_escrituracion`, `fecha_cancelacion`, `precio_total`, `monto_seña`, `monto_anticipos`, `monto_pagado`, `estado`, `porcentaje_pagado`, `id_usuario_creador`, `id_usuario_cancelador`, `fecha_creacion`, `fecha_modificacion`, `observaciones`, `motivo_cancelacion`) VALUES
(1, 5, 2, 5, '2025-10-05', NULL, NULL, 1000000.00, 100000.00, 500000.00, 1000000.00, 'seña_pagada', 190.00, 35, NULL, '2025-10-05 17:31:06', '2025-10-06 19:53:24', NULL, NULL),
(2, 54, 199, 40, '2025-10-05', NULL, NULL, 5000000.00, 500000.00, 1000000.00, 1533854.17, 'seña_pagada', 36.98, 35, NULL, '2025-10-05 17:35:05', '2025-10-07 09:33:59', 'probando un primer contrato de venta', NULL),
(3, 53, 381, 52, '2025-10-07', NULL, '2025-10-06', 2500000.00, 250000.00, 200000.00, 250000.00, 'cancelada', 10.00, 35, 35, '2025-10-06 04:58:41', '2025-10-06 08:08:40', 'probando el edit', 'probando la baja logica'),
(4, 53, 397, 52, '2025-10-06', NULL, '2025-10-06', 2500000.00, 500000.00, 1500000.00, 500000.00, 'cancelada', 0.00, 35, 35, '2025-10-06 14:32:58', '2025-10-06 15:16:44', 'probando la venta ', 'probando la redireccion'),
(5, 53, 2, 52, '2025-10-06', NULL, '2025-10-06', 2500000.00, 250000.00, 2000000.00, 250000.00, 'cancelada', 0.00, 35, 35, '2025-10-06 15:18:46', '2025-10-06 15:26:35', 'probando la redireccion', 'probando'),
(6, 76, 390, 72, '2025-10-10', '2025-10-30', NULL, 182000000.00, 18000000.00, 69000000.00, 43000000.00, 'seña_pagada', 37.36, 35, NULL, '2025-10-10 07:17:54', '2025-10-10 07:41:59', 'vendiendo el hospital', NULL),
(7, 72, 400, 69, '2025-10-10', '2025-10-30', NULL, 52054520.00, 5800000.00, 10000000.00, 5800000.00, 'seña_pagada', 0.00, 35, NULL, '2025-10-10 08:15:04', '2025-10-10 08:15:04', NULL, NULL),
(8, 77, 395, 73, '2025-10-10', '2025-10-30', NULL, 30000000.00, 3000000.00, 20000000.00, 5250000.00, 'seña_pagada', 25.00, 35, NULL, '2025-10-10 08:52:28', '2025-10-10 08:54:36', 'vendiendo la casa de la cultura', NULL),
(9, 82, 403, 75, '2025-10-10', '2025-10-13', NULL, 9999999999.99, 999999999.00, 5000000.00, 9999999999.99, 'seña_pagada', 190.00, 35, NULL, '2025-10-10 15:21:02', '2025-10-10 15:25:07', 'anticipo de 5000000. upro - 9 de julio 15', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `imagen_inmueble`
--

CREATE TABLE `imagen_inmueble` (
  `id_imagen` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `url` varchar(255) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `orden` int(11) DEFAULT 0,
  `fecha_creacion` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `imagen_inmueble`
--

INSERT INTO `imagen_inmueble` (`id_imagen`, `id_inmueble`, `url`, `descripcion`, `orden`, `fecha_creacion`) VALUES
(2, 5, '/images/inmuebles/a994281a-a4f7-4c00-ba97-d7a488431384.png', NULL, 1, '2025-09-02 09:09:03'),
(3, 5, '/images/inmuebles/5eae9637-f139-4dde-86b0-a1fafda6d4a0.png', NULL, 2, '2025-09-02 09:09:03'),
(4, 5, '/images/inmuebles/9dce2492-b481-4b70-a9de-f45f1bff997f.png', NULL, 3, '2025-09-02 09:09:03'),
(5, 5, '/images/inmuebles/6886b659-cc3a-4fca-87bb-4f74d5ea2ae6.png', NULL, 4, '2025-09-02 09:09:03'),
(8, 1, '/images/inmuebles/1/galeria/galeria_20250915_092358_f853456e.png', NULL, 1, '2025-09-15 09:23:58'),
(9, 1, '/images/inmuebles/1/galeria/galeria_20250915_092421_93abb4ad.jpg', NULL, 2, '2025-09-15 09:24:22'),
(10, 52, '/images/inmuebles/52/galeria/galeria_20250915_175042_8cc19d78.png', NULL, 1, '2025-09-15 17:50:42'),
(11, 52, '/images/inmuebles/52/galeria/galeria_20250915_195716_ee0f40c5.png', 'la casa de la esquina\r\n', 2, '2025-09-15 17:51:09'),
(12, 54, '/images/inmuebles/54/galeria/galeria_20250915_184234_f5c1fc73.png', NULL, 1, '2025-09-15 18:42:34'),
(13, 54, '/images/inmuebles/54/galeria/galeria_20250915_184253_87ec9c61.png', NULL, 2, '2025-09-15 18:42:53'),
(14, 54, '/images/inmuebles/54/galeria/galeria_20250915_184314_147372c4.png', NULL, 3, '2025-09-15 18:43:14'),
(17, 51, '/images/inmuebles/51/galeria/galeria_20251007_163407_199c344c.jpg', 'casa pra toda la familia', 1, '2025-10-07 16:34:07'),
(18, 6, '/images/inmuebles/6/galeria/galeria_20251007_163604_c693b569.jpg', NULL, 1, '2025-10-07 16:36:04'),
(19, 28, '/images/inmuebles/28/galeria/galeria_20251007_163658_1e343c09.jpg', NULL, 1, '2025-10-07 16:36:58'),
(20, 51, '/images/inmuebles/51/galeria/galeria_20251007_170054_3d94cded.jpg', NULL, 2, '2025-10-07 17:00:54'),
(21, 20, '/images/inmuebles/20/galeria/galeria_20251007_181541_8665da0e.jpg', NULL, 1, '2025-10-07 18:15:41'),
(22, 42, '/images/inmuebles/42/galeria/galeria_20251007_181633_4a93033c.jpg', NULL, 1, '2025-10-07 18:16:33'),
(23, 11, '/images/inmuebles/11/galeria/galeria_20251007_182116_df7912e5.jpg', NULL, 1, '2025-10-07 18:21:16'),
(24, 24, '/images/inmuebles/24/galeria/galeria_20251007_182304_355574ba.jpg', NULL, 1, '2025-10-07 18:23:04'),
(25, 14, '/images/inmuebles/14/galeria/galeria_20251007_182348_bd86fb5f.jpg', NULL, 1, '2025-10-07 18:23:48'),
(26, 39, '/images/inmuebles/39/galeria/galeria_20251007_182524_f9e41b00.jpg', NULL, 1, '2025-10-07 18:25:24'),
(27, 55, '/images/inmuebles/55/galeria/galeria_20251007_223456_bd55a2a3.jpg', NULL, 1, '2025-10-07 22:34:56'),
(28, 55, '/images/inmuebles/55/galeria/galeria_20251007_223509_a4aa53ef.jpg', NULL, 2, '2025-10-07 22:35:09'),
(29, 55, '/images/inmuebles/55/galeria/galeria_20251007_223522_d936aa8f.jpg', NULL, 3, '2025-10-07 22:35:22'),
(30, 55, '/images/inmuebles/55/galeria/galeria_20251007_223537_1f8fde6e.jpg', NULL, 4, '2025-10-07 22:35:37'),
(31, 55, '/images/inmuebles/55/galeria/galeria_20251007_223551_187817ce.jpg', NULL, 5, '2025-10-07 22:35:51'),
(32, 72, '/uploads/propiedades/72/galeria/galeria_c8f2052e-ac54-45ff-b9b6-e20d4777882f.jpg', NULL, 0, '2025-10-09 10:36:02'),
(33, 72, '/uploads/propiedades/72/galeria/galeria_993146a5-73f2-4657-8efd-a931076f9ab9.jpg', NULL, 0, '2025-10-09 10:36:02'),
(35, 72, '/uploads/propiedades/72/galeria/galeria_8b578e39-61be-4e59-92bb-40ef2264e3a9.jpg', NULL, 0, '2025-10-09 10:36:02'),
(36, 72, '/uploads/propiedades/72/galeria/galeria_1902fc29-e635-4372-87d8-a743db717d6b.jpg', NULL, 0, '2025-10-09 10:36:02'),
(37, 69, '/uploads/propiedades/69/galeria/galeria_eeff40d3-a278-4342-a9b8-e7301529e72d.jpg', NULL, 0, '2025-10-09 10:36:55'),
(38, 69, '/uploads/propiedades/69/galeria/galeria_3891bffd-4193-49bd-9426-8f3688b438f9.jpg', NULL, 0, '2025-10-09 10:36:56'),
(39, 69, '/uploads/propiedades/69/galeria/galeria_0001e79d-0b57-494f-b71d-559f4bcc6c24.jpg', NULL, 0, '2025-10-09 10:36:56'),
(40, 69, '/uploads/propiedades/69/galeria/galeria_845a0863-3020-41bc-b9e0-911ce23e0daf.jpg', NULL, 0, '2025-10-09 10:36:57'),
(41, 21, '/images/inmuebles/21/galeria/galeria_20251009_172352_dcd831c4.jpg', NULL, 1, '2025-10-09 17:23:52');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inmueble`
--

CREATE TABLE `inmueble` (
  `id_inmueble` int(11) NOT NULL,
  `id_propietario` int(11) NOT NULL,
  `id_tipo_inmueble` int(11) NOT NULL,
  `direccion` varchar(255) NOT NULL,
  `uso` enum('residencial','comercial') NOT NULL,
  `ambientes` int(11) NOT NULL,
  `precio` decimal(12,2) NOT NULL,
  `coordenadas` varchar(100) DEFAULT NULL,
  `url_portada` varchar(500) DEFAULT NULL,
  `estado` enum('disponible','no_disponible','alquilado','inactivo','Venta','Vendido','Reservado Alquiler','Reservado Venta') NOT NULL DEFAULT 'disponible',
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `id_usuario_creador` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inmueble`
--

INSERT INTO `inmueble` (`id_inmueble`, `id_propietario`, `id_tipo_inmueble`, `direccion`, `uso`, `ambientes`, `precio`, `coordenadas`, `url_portada`, `estado`, `fecha_alta`, `id_usuario_creador`) VALUES
(1, 1, 1, 'Av. Siempre Viva 742', 'residencial', 3, 50000.00, '-34.60337,-58.38162', '/images/inmuebles/1/portada/portada.png', 'disponible', '2025-08-22 19:19:05', NULL),
(2, 2, 1, 'Calle Comercial 123', 'comercial', 1, 80000.00, '-34.6083,-58.3712', '/images/inmuebles/2/portada/portada.jpg', 'disponible', '2025-08-22 19:19:05', NULL),
(3, 2, 1, 'ruta 698 calle 20', 'residencial', 4, 50000.00, '-34.6037,-58.3816', '/images/inmuebles/3/portada/portada.jpg', 'no_disponible', '2025-09-02 12:02:20', NULL),
(4, 1, 1, 'primavera 12456', 'residencial', 4, 800000.00, '-34.6030,-58.3815', NULL, 'inactivo', '2025-09-02 12:07:59', NULL),
(5, 5, 1, 'san luis 456', 'comercial', 5, 1000000.00, '-34.6017,-58.3812', '/images/inmuebles/5/portada/portada.jpg', 'disponible', '2025-09-02 12:09:03', NULL),
(6, 24, 1, 'Av. Test 1001, San Luis', 'residencial', 3, 85000.00, '-33.2950,-66.3356', NULL, 'inactivo', '2025-09-05 14:38:02', NULL),
(7, 23, 1, 'Belgrano Test 567, San Luis', 'residencial', 2, 65000.00, '-33.2951,-66.3357', NULL, 'inactivo', '2025-09-05 14:38:02', NULL),
(8, 22, 1, 'Mitre Test 8asdas90, Villa Mercedes', 'residencial', 4, 95000.00, '-33.6823,-65.4631', '/images/inmuebles/8/portada/portada.jpg', 'alquilado', '2025-09-05 14:38:02', NULL),
(9, 21, 1, 'Sarmiento Test 123', 'residencial', 3, 888000.00, '-33.2952,-66.3358', '/images/inmuebles/9/portada/portada.jpg', 'inactivo', '2025-09-05 14:38:02', NULL),
(10, 20, 1, 'Rivadavia Tasest 456, Villa Mercedes', 'residencial', 2, 70000.00, '-33.6824,-65.4632', '/images/inmuebles/10/portada/portada.jpg', 'alquilado', '2025-09-05 14:38:02', NULL),
(11, 19, 5, 'Colón Test 789, San Lui', 'comercial', 1, 120000.00, '-33.2953,-66.3359', '/images/inmuebles/11/portada/portada.jpg', 'alquilado', '2025-09-05 14:38:02', NULL),
(12, 24, 1, 'Pringles Test 3asdsa21, Villa Mercedes', 'residencial', 4, 88000.00, '-33.6825,-65.4633', '/images/inmuebles/12/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(13, 23, 5, 'Junín t 654, San Luis', 'comercial', 2, 110000.00, '-33.2954,-66.3360', '/images/inmuebles/13/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(14, 22, 1, 'Chacabuco 987, Villa Mercedes', 'residencial', 2, 68000.00, '-33.6826,-65.4634', '/images/inmuebles/14/portada/portada.jpg', 'inactivo', '2025-09-05 14:38:02', NULL),
(15, 21, 1, 'Independencia st 147, San Luis', 'residencial', 3, 82000.00, '-33.2955,-66.3361', '/images/inmuebles/15/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(16, 20, 1, 'Libertad Test 245648, Villa Mercedes', 'residencial', 5, 105000.00, '-33.6827,-65.4635', '/images/inmuebles/16/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(17, 19, 5, 'Constitución  369, San Luis', 'residencial', 1, 150000.00, '-33.2956,-66.3362', '/images/inmuebles/17/portada/portada.jpg', 'alquilado', '2025-09-05 14:38:02', NULL),
(18, 24, 1, 'Tucumán Test 7sd41, Villa Mercedes', 'residencial', 2, 72000.00, '-33.6828,-65.4636', '/images/inmuebles/18/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(19, 23, 5, 'Córdoba Test 8992, San Luis', 'comercial', 1, 95000.00, '-33.2957,-66.3363', '/images/inmuebles/19/portada/portada.jpg', 'no_disponible', '2025-09-05 14:38:02', NULL),
(20, 22, 1, 'Entre Ríos Test 968, Villa Mercedes', 'residencial', 3, 80000.00, '-33.6829,-65.4637', '/images/inmuebles/20/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(21, 21, 5, 'Santa Fe Teast 159, San Luis', 'comercial', 3, 125000.00, '-33.2958,-66.3364', '/images/inmuebles/21/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(22, 20, 1, 'Mendoza Test 2674545, Villa Mercedes', 'residencial', 4, 92000.00, '-33.6830,-65.4638', '/images/inmuebles/22/portada/portada.jpg', 'alquilado', '2025-09-05 14:38:02', NULL),
(23, 19, 5, 'La Rioja378, San Luis', 'residencial', 1, 45000.00, '-33.2959,-66.3365', '/images/inmuebles/23/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(24, 24, 1, 'Catamarca Test 489, Villa Mercedes', 'residencial', 2, 75000.00, '-33.6831,-65.4639', NULL, 'inactivo', '2025-09-05 14:38:02', NULL),
(25, 23, 1, 'Jujuy Tt 590, San Luis', 'residencial', 4, 998000.00, '-33.2960,-66.3366', '/images/inmuebles/25/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(26, 22, 5, 'Salta Test 6aa01, Villa Mercedes', 'comercial', 2, 115000.00, '-33.6832,-65.4640', '/images/inmuebles/26/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(27, 21, 1, 'Formosa T 712, San Luis', 'residencial', 3, 85000.00, '-33.2961,-66.3367', '/images/inmuebles/27/portada/portada.jpg', 'disponible', '2025-09-05 14:38:02', NULL),
(28, 55, 1, 'Av. Test 1001, San Luis', 'residencial', 3, 85000.00, '-33.2950,-66.3356', NULL, 'inactivo', '2025-09-05 14:44:48', NULL),
(29, 54, 1, 'Belgrano  567, San Luis', 'residencial', 2, 65000.00, '-33.2951,-66.3357', '/images/inmuebles/29/portada/portada.jpg', 'alquilado', '2025-09-05 14:44:48', NULL),
(30, 53, 1, 'Mitre Test 89asdas0, Villa Mercedes', 'residencial', 4, 95000.00, '-33.6823,-65.4631', '/images/inmuebles/30/portada/portada.jpg', 'alquilado', '2025-09-05 14:44:48', NULL),
(31, 52, 1, 'Sarmiento Test 123, San Luis', 'residencial', 3, 78000.00, '-33.2952,-66.3358', NULL, 'inactivo', '2025-09-05 14:44:48', NULL),
(32, 51, 1, 'Rivadavia Test aa456, Villa Mercedes', 'residencial', 2, 70000.00, '-33.6824,-65.4632', '/images/inmuebles/32/portada/portada.jpg', 'alquilado', '2025-09-05 14:44:48', NULL),
(33, 50, 5, 'Colón Test 789, San Luis', 'comercial', 1, 120000.00, '-33.2953,-66.3359', '/images/inmuebles/33/portada/portada.jpg', 'alquilado', '2025-09-05 14:44:48', NULL),
(34, 55, 1, 'Pringles Testaa 321, Villa Mercedes', 'residencial', 4, 88000.00, '-33.6825,-65.4633', '/images/inmuebles/34/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(35, 54, 5, 'Junín 654, San Luis', 'comercial', 2, 110000.00, '-33.2954,-66.3360', '/images/inmuebles/35/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(36, 53, 1, 'Chacabuco Test 987, Villa Mercedes', 'residencial', 2, 68000.00, '-33.6826,-65.4634', NULL, 'inactivo', '2025-09-05 14:44:48', NULL),
(37, 52, 1, 'Independenciast 147, San Luis', 'residencial', 3, 82000.00, '-33.2955,-66.3361', '/images/inmuebles/37/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(38, 3, 1, 'Libertad 4255558, Villa Mercedes', 'residencial', 5, 105000.00, '-33.6827,-65.4635', '/images/inmuebles/38/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(39, 50, 5, 'Constitución Te369, San Luis', 'residencial', 1, 150000.00, '-33.2956,-66.3362', '/images/inmuebles/39/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(40, 55, 1, 'Tucumán Teast 741, Villa Mercedes', 'residencial', 2, 72000.00, '-33.6828,-65.4636', '/images/inmuebles/40/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(41, 54, 5, 'Córdoba Tet 852, San Luis', 'comercial', 1, 95000.00, '-33.2957,-66.3363', '/images/inmuebles/41/portada/portada.jpg', 'no_disponible', '2025-09-05 14:44:48', NULL),
(42, 53, 1, 'Entre Ríos Test 963', 'residencial', 3, 80000.00, '-33.6829,-65.4637', '/images/inmuebles/42/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(43, 52, 5, 'Santa Fe Test 1asdas59, San Luis', 'comercial', 3, 125000.00, '-33.2958,-66.3364', '/images/inmuebles/43/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(44, 51, 1, 'Mendoza Test 2asdas67, Villa Mercedes', 'residencial', 4, 92000.00, '-33.6830,-65.4638', '/images/inmuebles/44/portada/portada.jpg', 'alquilado', '2025-09-05 14:44:48', NULL),
(45, 50, 5, 'La Rioja Test 378788, San Luis', 'residencial', 1, 45000.00, '-33.2959,-66.3365', '/images/inmuebles/45/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(46, 55, 1, 'Catamarca Test 489, Villa Mercedes', 'residencial', 2, 75000.00, '-33.6831,-65.4639', '/images/inmuebles/46/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(47, 54, 1, 'Jujuy Tes 590, San Luis', 'residencial', 4, 98000.00, '-33.2960,-66.3366', '/images/inmuebles/47/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(48, 53, 5, 'Salta Test 60aa1, Villa Mercedes', 'comercial', 2, 115000.00, '-33.6832,-65.4640', '/images/inmuebles/48/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(49, 52, 1, 'Formosa Tst 712, San Luis', 'residencial', 3, 85000.00, '-33.2961,-66.3367', '/images/inmuebles/49/portada/portada.jpg', 'disponible', '2025-09-05 14:44:48', NULL),
(51, 63, 13, 'la casa de mi abuela', 'comercial', 1, 5000000.00, '-34.6017,-58.3812', '/images/inmuebles/51/portada/portada.png', 'disponible', '2025-09-14 18:44:29', NULL),
(52, 9, 40, 'Av. Siempre Viva 601', 'comercial', 1, 300000.00, NULL, '/images/inmuebles/52/portada/portada.png', 'inactivo', '2025-09-15 20:09:59', NULL),
(53, 52, 42, 'san luis 1aa2345', 'comercial', 3, 2500000.00, NULL, '/images/inmuebles/53/portada/portada.jpg', 'Venta', '2025-09-15 20:19:46', NULL),
(54, 40, 40, 'Av. Siempre Viva 602', 'comercial', 1, 5000000.00, NULL, '/images/inmuebles/54/portada/portada.png', 'Venta', '2025-09-15 21:22:26', NULL),
(55, 64, 3, 'Venezuela 1234', 'comercial', 2, 4800000.00, NULL, '/images/inmuebles/55/portada/portada.png', 'alquilado', '2025-09-16 20:44:23', NULL),
(69, 69, 1, 'Casa Test Andrea', 'residencial', 4, 15820000.00, NULL, '/uploads/propiedades/69/portada/portada_e26017c4-58bb-435f-8965-38edc4982658.jpg', 'disponible', '2025-10-09 04:38:25', 390),
(72, 69, 47, 'casa del propietario', 'comercial', 3, 52054520.00, '-34.6037,-58.3816', '/images/inmuebles/72/portada/portada.jpg', 'disponible', '2025-10-09 11:26:44', NULL),
(73, 69, 3, 'segunda o tercera del propietario', '', 2, 30000000.00, '-34.6037,-58.3816', '/images/inmuebles/73/portada/portada.jpg', 'inactivo', '2025-10-09 14:23:47', NULL),
(74, 8, 46, 'municipalidad villa mercedes', '', 8, 8200000.00, '-33.674688,-65.461636', '/images/inmuebles/74/portada/portada.jpg', 'disponible', '2025-10-10 07:35:28', NULL),
(75, 71, 46, 'calle angosta ', 'comercial', 1, 32000000.00, '-33.658712,-65.453034', '/images/inmuebles/75/portada/portada.jpg', 'alquilado', '2025-10-10 08:04:53', NULL),
(76, 72, 41, 'hospital juan domingo peron', 'comercial', 4, 182000000.00, '-33.659498,-65.429220', '/images/inmuebles/76/portada/portada.jpg', 'Venta', '2025-10-10 08:21:08', NULL),
(77, 73, 3, 'casa de la cultura ', 'comercial', 3, 30000000.00, '-33.682590, -65.465689', '/images/inmuebles/77/portada/portada.jpg', 'Venta', '2025-10-10 11:49:15', NULL),
(78, 74, 5, 'banco nacion ', 'comercial', 3, 5000000.00, '-33.684668, -65.466151', '/images/inmuebles/78/portada/portada.jpg', 'alquilado', '2025-10-10 18:01:24', NULL),
(82, 75, 3, '9 de julio 15', '', 10, 9999999999.99, '-33.674693, -65.455117', '/images/inmuebles/82/portada/portada.jpg', 'Vendido', '2025-10-10 18:07:38', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inquilino`
--

CREATE TABLE `inquilino` (
  `id_inquilino` int(11) NOT NULL,
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1,
  `id_usuario` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inquilino`
--

INSERT INTO `inquilino` (`id_inquilino`, `fecha_alta`, `estado`, `id_usuario`) VALUES
(1, '2025-08-22 19:19:05', 1, 1),
(2, '2025-08-22 19:19:05', 1, 2),
(3, '2025-08-24 03:25:09', 1, 0),
(4, '2025-08-24 03:27:09', 1, 0),
(5, '2025-09-05 14:38:01', 1, 208),
(6, '2025-09-05 14:38:01', 1, 207),
(7, '2025-09-05 14:38:01', 1, 206),
(8, '2025-09-05 14:38:01', 1, 205),
(9, '2025-09-05 14:38:01', 1, 204),
(10, '2025-09-05 14:38:01', 1, 203),
(11, '2025-09-05 14:38:01', 1, 202),
(12, '2025-09-05 14:38:01', 1, 201),
(13, '2025-09-05 14:38:01', 1, 200),
(14, '2025-09-05 14:38:01', 1, 199),
(15, '2025-09-05 14:38:01', 1, 198),
(16, '2025-09-05 14:38:01', 1, 197),
(17, '2025-09-05 14:38:01', 1, 196),
(18, '2025-09-05 14:38:01', 1, 195),
(19, '2025-09-05 14:38:01', 1, 194),
(20, '2025-09-05 14:38:01', 1, 193),
(21, '2025-09-05 14:38:01', 1, 192),
(22, '2025-09-05 14:38:01', 1, 191),
(23, '2025-09-05 14:38:01', 1, 189),
(24, '2025-09-05 14:38:01', 1, 58),
(36, '2025-09-05 14:44:48', 1, 208),
(37, '2025-09-05 14:44:48', 1, 207),
(38, '2025-09-05 14:44:48', 1, 206),
(39, '2025-09-05 14:44:48', 1, 205),
(40, '2025-09-05 14:44:48', 1, 204),
(41, '2025-09-05 14:44:48', 1, 203),
(42, '2025-09-05 14:44:48', 1, 202),
(43, '2025-09-05 14:44:48', 0, 201),
(44, '2025-09-05 14:44:48', 0, 200),
(45, '2025-09-05 14:44:48', 0, 199),
(46, '2025-09-05 14:44:48', 0, 198),
(47, '2025-09-05 14:44:48', 0, 197),
(48, '2025-09-05 14:44:48', 0, 196),
(49, '2025-09-05 14:44:48', 0, 195),
(50, '2025-09-05 14:44:48', 0, 194),
(51, '2025-09-05 14:44:48', 0, 193),
(52, '2025-09-05 14:44:48', 0, 192),
(53, '2025-09-05 14:44:48', 0, 191),
(54, '2025-09-05 14:44:48', 0, 189),
(55, '2025-09-05 14:44:48', 0, 58),
(56, '2025-09-10 17:45:02', 0, 337),
(57, '2025-09-12 13:46:04', 0, 338),
(58, '2025-09-12 13:46:04', 0, 339),
(59, '2025-09-12 13:46:04', 0, 340),
(60, '2025-09-12 13:46:04', 0, 341),
(61, '2025-09-12 13:46:04', 0, 342),
(62, '2025-09-12 13:46:04', 0, 343),
(63, '2025-09-12 13:46:04', 0, 344),
(64, '2025-09-12 13:46:04', 0, 345),
(65, '2025-09-12 13:46:04', 0, 346),
(66, '2025-09-12 13:46:04', 0, 347),
(67, '2025-09-18 20:44:19', 0, 382),
(68, '2025-09-30 12:59:04', 0, 387),
(69, '2025-09-30 19:11:59', 1, 391),
(70, '2025-10-01 20:39:58', 1, 399),
(71, '2025-10-10 06:15:50', 1, 400),
(72, '2025-10-10 17:55:17', 1, 406);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `interes_inmueble`
--

CREATE TABLE `interes_inmueble` (
  `id_interes` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `email` varchar(100) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `fecha` datetime DEFAULT current_timestamp(),
  `contactado` tinyint(1) DEFAULT 0,
  `fecha_contacto` datetime DEFAULT NULL,
  `observaciones` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `interes_inmueble`
--

INSERT INTO `interes_inmueble` (`id_interes`, `id_inmueble`, `nombre`, `email`, `telefono`, `fecha`, `contactado`, `fecha_contacto`, `observaciones`) VALUES
(1, 1, 'Carlos', 'leandrosebastiantroncoso@gmail.com', '2657645466', '2025-09-01 10:34:08', 1, '2025-10-09 17:34:50', NULL),
(2, 1, 'Probando Guardado', 'leandro@gmail.com', '2657645466', '2025-09-01 10:35:31', 1, '2025-10-09 17:34:50', NULL),
(3, 5, 'andrea', 'andre@gmail.com', '2657645845', '2025-09-04 14:44:08', 1, '2025-10-09 23:24:28', NULL),
(4, 43, 'leandro', 'leandro@gmail.com', '255465456', '2025-10-09 11:19:19', 1, '2025-10-09 17:33:32', NULL),
(5, 35, 'maliita', 'cabmaga@gmail.com', '2567656565', '2025-10-09 23:02:17', 0, NULL, 'alquilar');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `pago`
--

CREATE TABLE `pago` (
  `id_pago` int(11) NOT NULL,
  `id_contrato` int(11) DEFAULT NULL,
  `id_inmueble` int(11) NOT NULL,
  `tipo_pago` varchar(20) NOT NULL DEFAULT 'alquiler',
  `numero_pago` int(11) NOT NULL,
  `fecha_pago` date NOT NULL,
  `fecha_vencimiento` date DEFAULT NULL,
  `periodo_pago` varchar(7) DEFAULT NULL,
  `periodo_año` int(11) DEFAULT NULL,
  `periodo_mes` int(11) DEFAULT NULL,
  `concepto` varchar(200) NOT NULL,
  `monto_base` decimal(12,2) NOT NULL,
  `recargo_mora` decimal(12,2) NOT NULL DEFAULT 0.00,
  `monto_total` decimal(12,2) NOT NULL DEFAULT 0.00,
  `dias_mora` int(11) DEFAULT 0,
  `monto_diario_mora` decimal(10,2) DEFAULT NULL,
  `estado` enum('pendiente','pagado','anulado','activo') NOT NULL DEFAULT 'pendiente',
  `id_usuario_creador` int(11) NOT NULL,
  `id_usuario_anulador` int(11) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `fecha_anulacion` timestamp NULL DEFAULT NULL,
  `comprobante_ruta` varchar(500) DEFAULT NULL,
  `comprobante_nombre` varchar(255) DEFAULT NULL,
  `observaciones` varchar(500) DEFAULT NULL,
  `id_contrato_venta` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `pago`
--

INSERT INTO `pago` (`id_pago`, `id_contrato`, `id_inmueble`, `tipo_pago`, `numero_pago`, `fecha_pago`, `fecha_vencimiento`, `periodo_pago`, `periodo_año`, `periodo_mes`, `concepto`, `monto_base`, `recargo_mora`, `monto_total`, `dias_mora`, `monto_diario_mora`, `estado`, `id_usuario_creador`, `id_usuario_anulador`, `fecha_creacion`, `fecha_anulacion`, `comprobante_ruta`, `comprobante_nombre`, `observaciones`, `id_contrato_venta`) VALUES
(3, 2, 2, 'alquiler', 1, '2024-02-10', NULL, NULL, NULL, NULL, 'Mes febrero', 80000.00, 0.00, 80000.00, 0, NULL, 'anulado', 2, 1, '2025-08-22 19:19:05', '2025-10-04 23:31:06', NULL, NULL, NULL, NULL),
(4, 7, 29, 'alquiler', 1, '2025-10-02', '2025-11-10', NULL, NULL, NULL, 'Alquiler #01 - Belgrano Test 567, San Luis', 1000.00, 0.00, 1000.00, 0, 50.00, 'pagado', 1, NULL, '2025-10-02 22:44:25', NULL, NULL, NULL, 'probando', NULL),
(5, 7, 29, 'alquiler', 2, '2025-10-02', '2025-11-10', NULL, NULL, NULL, 'Alquiler #02 - Belgrano Test 567, San Luis', 1000.00, 0.00, 1000.00, 0, 50.00, 'pagado', 1, NULL, '2025-10-03 00:27:52', NULL, NULL, NULL, NULL, NULL),
(6, 7, 29, 'alquiler', 3, '2025-10-03', '2025-11-10', NULL, NULL, NULL, 'Alquiler #03 - Belgrano Test 567, San Luis', 1000.00, 0.00, 1000.00, 0, 50.00, 'pagado', 1, NULL, '2025-10-03 10:07:44', NULL, NULL, NULL, NULL, NULL),
(7, 7, 29, 'alquiler', 4, '2025-10-03', '2025-11-10', NULL, NULL, NULL, 'Alquiler #04 - Belgrano Test 567, San Luis', 1000.00, 0.00, 1000.00, 0, 50.00, 'anulado', 1, 1, '2025-10-03 10:09:35', '2025-10-04 23:03:41', NULL, NULL, NULL, NULL),
(8, 18, 29, 'alquiler', 1, '2025-10-04', '2025-12-10', NULL, NULL, NULL, 'Alquiler noviembre 2025 - Belgrano Test 567, San Luis', 1000.01, 0.00, 1000.01, 0, 50.00, 'pagado', 1, NULL, '2025-10-05 02:03:41', NULL, NULL, NULL, 'probando pago de otro unmueble con comprobante', NULL),
(24, NULL, 5, 'venta', 0, '2025-10-05', NULL, NULL, NULL, NULL, 'Pago total - Contrato de Venta', 900000.00, 0.00, 900000.00, 0, NULL, 'anulado', 35, 35, '2025-10-06 22:53:24', '2025-10-07 01:22:14', NULL, NULL, 'probando el pago ', 1),
(25, NULL, 54, 'venta', 1, '2025-10-07', NULL, NULL, NULL, NULL, 'Pago de cuota - Plan de pagos', 375000.00, 0.00, 375000.00, 0, NULL, 'pagado', 35, NULL, '2025-10-07 12:29:37', NULL, NULL, NULL, 'probando las cuotas', 2),
(26, NULL, 54, 'venta', 2, '2025-10-07', NULL, NULL, NULL, NULL, 'Pago de cuota - Plan de pagos', 343750.00, 0.00, 343750.00, 0, NULL, 'pagado', 35, NULL, '2025-10-07 12:31:13', NULL, NULL, NULL, NULL, 2),
(27, NULL, 54, 'venta', 3, '2025-10-07', NULL, NULL, NULL, NULL, 'Pago de cuota - Plan de pagos', 315104.17, 0.00, 315104.17, 0, NULL, 'pagado', 35, NULL, '2025-10-07 12:33:59', NULL, NULL, NULL, NULL, 2),
(28, 23, 75, 'alquiler', 1, '2025-10-10', '2025-11-10', NULL, NULL, NULL, 'Alquiler octubre 2025 - calle angosta ', 4820000.00, 0.00, 4820000.00, 0, 50.00, 'pagado', 35, NULL, '2025-10-10 10:39:43', NULL, NULL, NULL, 'probando un nuevo pago', NULL),
(29, NULL, 76, 'venta', 1, '2025-10-10', NULL, NULL, NULL, NULL, 'Anticipo de compra - Contrato de Venta', 25000000.00, 0.00, 25000000.00, 0, NULL, 'pagado', 35, NULL, '2025-10-10 10:41:59', NULL, NULL, NULL, NULL, 6),
(30, NULL, 77, 'venta', 1, '2025-10-10', NULL, NULL, NULL, NULL, 'Pago de cuota - Plan de pagos', 2250000.00, 0.00, 2250000.00, 0, NULL, 'pagado', 35, NULL, '2025-10-10 11:54:36', NULL, NULL, NULL, NULL, 8),
(31, NULL, 82, 'venta', 1, '2025-10-10', NULL, NULL, NULL, NULL, 'Pago total - Contrato de Venta', 9000000000.99, 0.00, 9000000000.99, 0, NULL, 'pagado', 35, NULL, '2025-10-10 18:25:07', NULL, '/uploads/comprobantes/venta_20251010_152506_e4531b41.jpg', 'descarga5.jpg', NULL, 9);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `propietario`
--

CREATE TABLE `propietario` (
  `id_propietario` int(11) NOT NULL,
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1,
  `id_usuario` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `propietario`
--

INSERT INTO `propietario` (`id_propietario`, `fecha_alta`, `estado`, `id_usuario`) VALUES
(1, '2025-08-22 19:19:05', 1, 1),
(2, '2025-08-22 19:19:05', 1, 2),
(3, '2025-08-24 03:25:32', 1, 3),
(4, '2025-08-24 03:27:33', 0, 0),
(5, '2025-08-24 03:31:09', 1, 4),
(6, '2025-08-31 12:21:20', 1, 8),
(7, '2025-09-05 14:38:01', 1, 34),
(8, '2025-09-05 14:38:01', 1, 35),
(9, '2025-09-05 14:38:01', 1, 36),
(10, '2025-09-05 14:38:01', 1, 37),
(11, '2025-09-05 14:38:01', 0, 38),
(12, '2025-09-05 14:38:01', 1, 39),
(13, '2025-09-05 14:38:01', 1, 40),
(14, '2025-09-05 14:38:01', 1, 41),
(15, '2025-09-05 14:38:01', 1, 42),
(16, '2025-09-05 14:38:01', 1, 43),
(17, '2025-09-05 14:38:01', 1, 44),
(18, '2025-09-05 14:38:01', 1, 45),
(19, '2025-09-05 14:38:01', 1, 46),
(20, '2025-09-05 14:38:01', 1, 47),
(21, '2025-09-05 14:38:01', 1, 48),
(22, '2025-09-05 14:38:01', 1, 49),
(23, '2025-09-05 14:38:01', 1, 50),
(24, '2025-09-05 14:38:01', 1, 51),
(38, '2025-09-05 14:44:48', 1, 34),
(39, '2025-09-05 14:44:48', 1, 35),
(40, '2025-09-05 14:44:48', 1, 36),
(41, '2025-09-05 14:44:48', 1, 37),
(42, '2025-09-05 14:44:48', 1, 38),
(43, '2025-09-05 14:44:48', 1, 39),
(44, '2025-09-05 14:44:48', 0, 40),
(45, '2025-09-05 14:44:48', 0, 41),
(46, '2025-09-05 14:44:48', 0, 42),
(47, '2025-09-05 14:44:48', 0, 43),
(48, '2025-09-05 14:44:48', 0, 44),
(49, '2025-09-05 14:44:48', 0, 45),
(50, '2025-09-05 14:44:48', 0, 46),
(51, '2025-09-05 14:44:48', 0, 47),
(52, '2025-09-05 14:44:48', 0, 48),
(53, '2025-09-05 14:44:48', 0, 49),
(54, '2025-09-05 14:44:48', 0, 50),
(55, '2025-09-05 14:44:48', 0, 51),
(56, '2025-09-10 16:45:18', 0, 335),
(57, '2025-09-10 16:56:48', 0, 336),
(58, '2025-09-12 13:46:05', 0, 338),
(59, '2025-09-12 13:46:05', 0, 340),
(60, '2025-09-12 13:46:05', 0, 342),
(61, '2025-09-12 13:46:05', 0, 344),
(62, '2025-09-12 13:46:05', 0, 346),
(63, '2025-09-14 11:30:42', 0, 378),
(64, '2025-09-16 20:42:33', 0, 379),
(65, '2025-09-18 20:40:28', 0, 380),
(66, '2025-09-18 20:42:02', 0, 381),
(67, '2025-09-30 13:01:45', 1, 388),
(68, '2025-09-30 13:20:35', 0, 389),
(69, '2025-09-30 19:10:15', 1, 390),
(70, '2025-10-01 20:03:08', 1, 398),
(71, '2025-10-10 06:19:36', 1, 401),
(72, '2025-10-10 08:07:40', 1, 402),
(73, '2025-10-10 11:45:51', 1, 403),
(74, '2025-10-10 17:52:24', 1, 404),
(75, '2025-10-10 17:53:59', 1, 405);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `tipo_inmueble`
--

CREATE TABLE `tipo_inmueble` (
  `id_tipo_inmueble` int(11) NOT NULL,
  `nombre` varchar(50) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `tipo_inmueble`
--

INSERT INTO `tipo_inmueble` (`id_tipo_inmueble`, `nombre`, `descripcion`, `fecha_creacion`, `estado`) VALUES
(1, 'Departamento', 'Unidad residencial en edificio', '2025-08-22 19:19:05', 1),
(2, 'Casa', 'Vivienda unifamiliar', '2025-08-22 19:19:05', 1),
(3, 'Local', 'Espacio comercial', '2025-08-22 19:19:05', 1),
(4, 'Depósito', 'Almacén o bodega', '2025-08-22 19:19:05', 1),
(5, 'Oficina', 'Espacio de trabajo comercial', '2025-08-22 19:19:05', 1),
(10, 'Local Comercial', 'Espacio destinado a actividad comercial', '2025-09-05 14:33:11', 1),
(12, 'Galpón', 'Espacio amplio para depósito o industria', '2025-09-05 14:33:11', 1),
(13, 'Terreno', 'Lote baldío para construcción', '2025-09-05 14:33:11', 1),
(17, 'PH (Propiedad Horizontal)', 'Casa en propiedad horizontal', '2025-09-05 14:38:00', 1),
(38, 'En Construccion', 'Vivienda de dos plantas conectadas', '2025-09-12 13:46:05', 1),
(39, 'Cochera', 'Espacio para estacionamiento de vehículos', '2025-09-12 13:46:05', 1),
(40, 'Quinta', 'Propiedad rural con casa y terreno amplio', '2025-09-12 13:46:05', 1),
(41, 'Consultorio', 'Espacio para atención médica o profesional', '2025-09-12 13:46:05', 1),
(42, 'Loft', 'Espacio amplio sin divisiones internas', '2025-09-15 13:52:01', 1),
(43, 'Studio', 'Ambiente único que combina dormitorio y living', '2025-09-15 13:52:01', 1),
(44, 'Cabaña', 'Vivienda rústica, generalmente en zona rural', '2025-09-15 13:52:01', 1),
(45, 'Barracón', 'Edificio industrial o de almacenamiento', '2025-09-15 13:52:01', 1),
(46, 'Salón de Eventos', 'Espacio para celebraciones', '2025-09-15 13:52:01', 1),
(47, 'Gimnasio', 'Local para actividad física', '2025-09-15 13:52:01', 1),
(48, 'Restaurante', 'Local gastronómico', '2025-09-15 13:52:01', 1),
(49, 'Taller', 'Espacio para actividades manuales o mecánicas', '2025-09-15 13:52:01', 1),
(50, 'Bodega', 'Espacio de almacenamiento comercial', '2025-09-15 13:52:01', 1),
(51, 'casa de comidas', 'una casa para hacer comidas ', '2025-10-01 12:17:01', 0);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `usuario`
--

CREATE TABLE `usuario` (
  `id_usuario` int(11) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password` varchar(255) NOT NULL,
  `rol` varchar(20) NOT NULL DEFAULT 'empleado',
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `telefono` varchar(20) NOT NULL,
  `estado` varchar(20) NOT NULL DEFAULT 'activo',
  `dni` varchar(20) NOT NULL,
  `direccion` varchar(255) NOT NULL,
  `avatar` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `usuario`
--

INSERT INTO `usuario` (`id_usuario`, `email`, `password`, `rol`, `nombre`, `apellido`, `telefono`, `estado`, `dni`, `direccion`, `avatar`) VALUES
(1, 'juan.perez@inmobiliaria.com', '$2a$11$8YcxZN7K8ZoOw1pR3tCVke1lHzq8jF8qfJvKZwOuLrTl7n5dVJ3Zy', 'empleado', 'JeanCarlos', 'Pérez', '123456789', 'activo', '30123455', 'con dirección especificada', NULL),
(2, 'empleado1@inmobiliaria.com', 'ccc13e8ab0819e3ab61719de4071ecae6c1d3cd35dc48b91cad3481f20922f9f', 'empleado', 'MaríaMarta', 'Gómez', '987654321', 'activo', '30234567', 'Dirección no especificada', NULL),
(3, 'carlos.propietario@email.com', 'hashed_password_1', 'empleado', 'Carlos', 'García', '111222333', 'activo', '30345678', 'Dirección no especificada', NULL),
(4, 'ana.propietario@email.com', 'hashed_password_2', 'empleado', 'Ana', 'López', '444555666', 'activo', '30456789', 'Dirección no especificada', NULL),
(5, 'leandro.propietario@email.com', 'hashed_password_3', 'empleado', 'Leandro', 'Troncoso', '2657645466', 'activo', '30567890', 'Dirección no especificada', NULL),
(6, 'laura.propietario@email.com', 'hashed_password_4', 'empleado', 'Laura', 'Pérez', '555444333', 'activo', '30678901', 'Dirección no especificada', NULL),
(7, 'pedro.propietario@email.com', 'hashed_password_5', 'empleado', 'Pedro', 'Gómez', '666777888', 'activo', '30789012', 'Dirección no especificada', NULL),
(8, 'sin-email@dominio.com', '$2a$11$lp/yvIGUwY3GrmVHSiDlCOOGdi4C8IJoRuxOyB6/AQ2GMAQTzR3TW', 'propietario', 'Sin nombre', 'Sin apellido', 'Sin especificar', '1', '00000000', 'Sin dirección especificada', NULL),
(34, 'test1@inmobiliaria.com', '$2a$11$5GlZ5Z8ZqMZ5Z8ZqMZ5Z8O5GlZ5Z8ZqMZ5Z8ZqMZ5Z8ZqMZ5Z8ZqM', 'empleado', 'Roberto', 'Martín', '2664-101010', 'activo', '30123456789', 'Av. Pringles 1001, San Luis', NULL),
(35, 'test2@inmobiliaria.com', '$2a$11$pSNffxhGzTp00OPW6LEcdO/YwwdULF3xCzq/SHa0lqLEsG4/YyJl.', 'administrador', 'Leandro', 'Troncoso', '2664-202020', 'activo', '31234567890', 'Belgrano 1102, San Luis', NULL),
(36, 'test3@inmobiliaria.com', 'Test123', 'empleado', 'Daniel', 'Fernández', '2664-303030', 'activo', '32345678901', 'Mitre 1203, Villa Mercedes', NULL),
(37, 'test4@inmobiliaria.com', 'Test123', 'empleado', 'Claudia', 'Vega', '2664-404040', 'activo', '33456789012', 'Sarmiento 1304, San Luis', NULL),
(38, 'test5@inmobiliaria.com', 'Test123', 'empleado', 'Marcelo', 'Ríos', '2664-505050', 'inactivo', '34567890123', 'Rivadavia 1405, Villa Mercedes', NULL),
(39, 'test6@inmobiliaria.com', 'Test123', 'empleado', 'Silvia', 'Cabrera', '2664-606060', 'activo', '35678901234', 'Colón 1506, San Luis', NULL),
(40, 'test7@inmobiliaria.com', 'Test123', 'empleado', 'Gustavo', 'Moreno', '2664-707070', 'inactivo', '36789012345', 'Pringles 1607, Villa Mercedes', NULL),
(41, 'test8@inmobiliaria.com', 'Test123', 'empleado', 'Liliana', 'Sosa', '2664-808080', 'inactivo', '37890123456', 'Junín 1708, San Luis', NULL),
(42, 'test9@inmobiliaria.com', 'Test123', 'empleado', 'Adrián', 'Blanco', '2664-909090', 'inactivo', '38901234567', 'Chacabuco 1809, Villa Mercedes', NULL),
(43, 'test10@inmobiliaria.com', 'Test123', 'empleado', 'Beatriz', 'Navarro', '2664-101111', 'inactivo', '39012345678', 'Independencia 1910, San Luis', NULL),
(44, 'test11@inmobiliaria.com', 'Test123', 'empleado', 'Sergio', 'Paz', '2664-111212', 'inactivo', '30123456780', 'Libertad 2011, Villa Mercedes', NULL),
(45, 'test12@inmobiliaria.com', 'Test123', 'empleado', 'Nora', 'Álvarez', '2664-121313', 'inactivo', '31234567891', 'Constitución 2112, San Luis', NULL),
(46, 'test13@inmobiliaria.com', 'Test123', 'empleado', 'Rubén', 'Guerrero', '2664-131414', 'inactivo', '32345678902', 'Tucumán 2213, Villa Mercedes', NULL),
(47, 'test14@inmobiliaria.com', 'Test123', 'empleado', 'Gloria', 'Romero', '2664-141515', 'inactivo', '33456789013', 'Córdoba 2314, San Luis', NULL),
(48, 'test15@inmobiliaria.com', 'Test123', 'empleado', 'Oscar', 'Molina', '2664-151616', 'inactivo', '34567890124', 'Entre Ríos 2415, Villa Mercedes', NULL),
(49, 'test16@inmobiliaria.com', 'Test123', 'empleado', 'Marta', 'Benítez', '2664-161717', 'inactivo', '35678901235', 'Santa Fe 2516, San Luis', NULL),
(50, 'test17@inmobiliaria.com', 'Test123', 'empleado', 'Raúl', 'Campos', '2664-171818', 'inactivo', '36789012346', 'Mendoza 2617, Villa Mercedes', NULL),
(51, 'test18@inmobiliaria.com', 'Test123', 'empleado', 'Cristina', 'Valdez', '2664-181919', 'inactivo', '37890123457', 'La Rioja 2718, San Luis', NULL),
(52, 'test19@inmobiliaria.com', 'Test123', 'empleado', 'Eduardo', 'Rojas', '2664-192020', 'activo', '38901234568', 'Catamarca 2819, Villa Mercedes', NULL),
(53, 'test20@inmobiliaria.com', 'Test123', 'empleado', 'Rosa', 'Figueroa', '2664-202121', 'activo', '39012345679', 'Jujuy 2920, San Luis', NULL),
(54, 'test21@inmobiliaria.com', 'Test123', 'empleado', 'Pablo', 'Acosta', '2664-212222', 'activo', '30123456781', 'Salta 3021, Villa Mercedes', NULL),
(55, 'test22@inmobiliaria.com', 'Test123', 'empleado', 'Elena', 'Cortés', '2664-222323', 'activo', '31234567892', 'Formosa 3122, San Luis', NULL),
(56, 'test23@inmobiliaria.com', 'Test123', 'empleado', 'Héctor', 'Espinoza', '2664-232424', 'activo', '32345678903', 'Neuquén 3223, Villa Mercedes', NULL),
(57, 'test24@inmobiliaria.com', 'Test123', 'empleado', 'Irma', 'Peña', '2664-242525', 'activo', '33456789014', 'Río Negro 3324, San Luis', NULL),
(58, 'test25@inmobiliaria.com', 'Test123', 'empleado', 'Víctor', 'Mendoza', '2664-252626', 'inactivo', '34567890125', 'Chubut 3425, Villa Mercedes', NULL),
(184, 'testt1@sadasinmobiliariaa.com', 'Test123', 'empleado', 'Roberto', 'Martín', '2664-101010', 'activo', '30123456789', 'Av. Pringles 1001, San Luis', NULL),
(185, 'test2@inmobiliarias.com', 'Test123', 'empleado', 'Sandra', 'González', '2664-202020', 'activo', '31234567890', 'Belgrano 1102, San Luis', NULL),
(186, 'test3@inmobiliarida.com', 'Test123', 'empleado', 'Daniel', 'Fernández', '2664-303030', 'activo', '32345678901', 'Mitre 1203, Villa Mercedes', NULL),
(187, 'test4@inmobiliariac.com', 'Test123', 'empleado', 'Claudia', 'Vega', '2664-404040', 'activo', '33456789012', 'Sarmiento 1304, San Luis', NULL),
(188, 'test5@inmobiliarissa.com', 'Test123', 'empleado', 'Marcelo', 'Ríos', '2664-505050', 'activo', '34567890123', 'Rivadavia 1405, Villa Mercedes', NULL),
(189, 'test06@inmobiliaria.com', 'Test123', 'empleado', 'Silvia', 'Cabrera', '2664-606060', 'inactivo', '35678901234', 'Colón 1506, San Luis', NULL),
(190, 'test07@inmobiliariaa.com', 'Test123', 'empleado', 'Gustavo', 'Moreno', '2664-707070', 'activo', '36789012345', 'Pringles 1607, Villa Mercedes', NULL),
(191, 'test08@inmobiliaria.com', 'Test123', 'empleado', 'Liliana', 'Sosa', '2664-808080', 'inactivo', '37890123456', 'Junín 1708, San Luis', NULL),
(192, 'test09@inmobiliaria.com', 'Test123', 'empleado', 'Adrián', 'Blanco', '2664-909090', 'inactivo', '38901234567', 'Chacabuco 1809, Villa Mercedes', NULL),
(193, 'test010@inmobiliaria.com', 'Test123', 'empleado', 'Beatriz', 'Navarro', '2664-101111', 'inactivo', '39012345678', 'Independencia 1910, San Luis', NULL),
(194, 'test011@inmobiliaria.com', 'Test123', 'empleado', 'Sergio', 'Paz', '2664-111212', 'inactivo', '30123456780', 'Libertad 2011, Villa Mercedes', NULL),
(195, 'test012@inmobiliaria.com', 'Test123', 'empleado', 'Nora', 'Álvarez', '2664-121313', 'inactivo', '31234567891', 'Constitución 2112, San Luis', NULL),
(196, 'test013@inmobiliaria.com', 'Test123', 'empleado', 'Rubén', 'Guerrero', '2664-131414', 'inactivo', '32345678902', 'Tucumán 2213, Villa Mercedes', NULL),
(197, 'test014@inmobiliaria.com', 'Test123', 'empleado', 'Gloria', 'Romero', '2664-141515', 'inactivo', '33456789013', 'Córdoba 2314, San Luis', NULL),
(198, 'test015@inmobiliaria.com', 'Test123', 'empleado', 'Oscar', 'Molina', '2664-151616', 'inactivo', '34567890124', 'Entre Ríos 2415, Villa Mercedes', NULL),
(199, 'test016@inmobiliaria.com', 'Test123', 'empleado', 'Marta', 'Benítez', '2664-161717', 'inactivo', '35678901235', 'Santa Fe 2516, San Luis', NULL),
(200, 'test017@inmobiliaria.com', 'Test123', 'empleado', 'Raúl', 'Campos', '2664-171818', 'inactivo', '36789012346', 'Mendoza 2617, Villa Mercedes', NULL),
(201, 'test018@inmobiliaria.com', 'Test123', 'empleado', 'Cristinaa', 'Valdez', '2664-181919', 'inactivo', '37890123445', 'La Rioja 2718, San Luis', NULL),
(202, 'test019@inmobiliaria.com', 'Test123', 'empleado', 'Eduardo', 'Rojas', '2664-192020', 'activo', '38901234568', 'Catamarca 2819, Villa Mercedes', NULL),
(203, 'test020@inmobiliaria.com', 'Test123', 'empleado', 'Rosa', 'Figueroa', '2664-202121', 'activo', '39012345679', 'Jujuy 2920, San Luis', NULL),
(204, 'test021@inmobiliaria.com', 'Test123', 'empleado', 'Pablo', 'Acosta', '2664-212222', 'activo', '30123456781', 'Salta 3021, Villa Mercedes', NULL),
(205, 'test022@inmobiliaria.com', 'Test123', 'empleado', 'Elena', 'Cortés', '2664-222323', 'activo', '31234567892', 'Formosa 3122, San Luis', NULL),
(206, 'test023@inmobiliaria.com', 'Test123', 'empleado', 'Héctor', 'Espinoza', '2664-232424', 'activo', '32345678903', 'Neuquén 3223, Villa Mercedes', NULL),
(207, 'test024@inmobiliaria.com', 'Test123', 'empleado', 'Irma', 'Peña', '2664-242525', 'activo', '33456789014', 'Río Negro 3324, San Luis', NULL),
(208, 'test025@inmobiliaria.com', 'Test123', 'empleado', 'Víctor', 'Mendoza', '2664-252626', 'activo', '34567890125', 'Chubut 3425, Villa Mercedes', NULL),
(284, 'test01@sadasinmobiliariaa.com', 'Test123', 'empleado', 'Roberto', 'Martín', '2664-101010', 'activo', '30123456789', 'Av. Pringles 1001, San Luis', NULL),
(285, 'test02@inmobiliarias.com', 'Test123', 'empleado', 'Sandra', 'González', '2664-202020', 'eliminado', '31234567890', 'Belgrano 1102, San Luis', NULL),
(286, 'test03@inmobiliarida.com', 'Test123', 'empleado', 'Daniel', 'Fernández', '2664-303030', 'activo', '32345678901', 'Mitre 1203, Villa Mercedes', NULL),
(287, 'test04@inmobiliariac.com', 'Test123', 'empleado', 'Claudia', 'Vega', '2664-404040', 'activo', '33456789012', 'Sarmiento 1304, San Luis', NULL),
(288, 'test05@inmobiliarissa.com', 'Test123', 'empleado', 'Marcelo', 'Ríos', '2664-505050', 'activo', '34567890123', 'Rivadavia 1405, Villa Mercedes', NULL),
(289, 'test006@inmobiliaria01.com', 'Test123', 'empleado', 'Silvia', 'Cabrera', '2664-606060', 'activo', '35678901234', 'Colón 1506, San Luis', NULL),
(290, 'test07@inmobiliariaa02.com', 'Test123', 'empleado', 'Gustavo', 'Moreno', '2664-707070', 'activo', '36789012345', 'Pringles 1607, Villa Mercedes', NULL),
(291, 'test08@inmobiliaria03.com', 'Test123', 'empleado', 'Liliana', 'Sosa', '2664-808080', 'activo', '37890123456', 'Junín 1708, San Luis', NULL),
(292, 'test09@inmobiliaria04.com', 'Test123', 'empleado', 'AdriánRamirez1', 'Blanco', '2664-909090', 'activo', '38901234567', 'Chacabuco 1809, Villa Mercedes', '/avatars/avatar_292_4f06463f-23b5-4a88-a9b3-c8d905f38f69.jpg'),
(293, 'test010@inmobiliaria05.com', 'Test123', 'empleado', 'Beatriz', 'Navarro', '2664-101111', 'activo', '39012345678', 'Independencia 1910, San Luis', NULL),
(294, 'test011@inmobiliaria06.com', 'Test123', 'empleado', 'Sergio', 'Paz', '2664-111212', 'inactivo', '30123456780', 'Libertad 2011, Villa Mercedes', NULL),
(295, 'test012@inmobiliaria07.com', 'Test123', 'empleado', 'Nora', 'Álvarez', '2664-121313', 'activo', '31234567891', 'Constitución 2112, San Luis', NULL),
(296, 'test013@inmobiliaria08.com', 'Test123', 'empleado', 'Rubén', 'Guerrero', '2664-131414', 'activo', '32345678902', 'Tucumán 2213, Villa Mercedes', NULL),
(297, 'test014@inmobiliaria09.com', 'Test123', 'empleado', 'Gloria', 'Romero', '2664-141515', 'activo', '33456789013', 'Córdoba 2314, San Luis', NULL),
(298, 'test015@inmobiliaria10.com', 'Test123', 'empleado', 'Oscar', 'Molina', '2664-151616', 'activo', '34567890124', 'Entre Ríos 2415, Villa Mercedes', NULL),
(299, 'test016@inmobiliaria11.com', 'Test123', 'empleado', 'Marta', 'Benítez', '2664-161717', 'activo', '35678901235', 'Santa Fe 2516, San Luis', NULL),
(300, 'test017@inmobiliaria12.com', 'Test123', 'empleado', 'Raúl', 'Campos', '2664-171818', 'activo', '36789012346', 'Mendoza 2617, Villa Mercedes', NULL),
(301, 'test018@inmobiliaria13.com', 'Test123', 'empleado', 'Cristina', 'Valdez', '2664-181919', 'activo', '37890123457', 'La Rioja 2718, San Luis', NULL),
(302, 'test019@inmobiliaria14.com', 'Test123', 'empleado', 'Eduardo', 'Rojas', '2664-192020', 'activo', '38901234568', 'Catamarca 2819, Villa Mercedes', NULL),
(303, 'test020@inmobiliaria15.com', 'Test123', 'empleado', 'Rosa', 'Figueroa', '2664-202121', 'activo', '39012345679', 'Jujuy 2920, San Luis', NULL),
(304, 'test021@inmobiliaria16.com', 'Test123', 'empleado', 'Pablo', 'Acosta', '2664-212222', 'activo', '30123456781', 'Salta 3021, Villa Mercedes', NULL),
(305, 'test022@inmobiliaria17.com', 'Test123', 'empleado', 'Elena', 'Cortés', '2664-222323', 'activo', '31234567892', 'Formosa 3122, San Luis', NULL),
(306, 'test023@inmobiliaria18.com', 'Test123', 'empleado', 'Héctor', 'Espinoza', '2664-232424', 'activo', '32345678903', 'Neuquén 3223, Villa Mercedes', NULL),
(307, 'test024@inmobiliaria19.com', 'Test123', 'empleado', 'Irma', 'Peña', '2664-242525', 'activo', '33456789014', 'Río Negro 3324, San Luis', NULL),
(308, 'test025@inmobiliaria20.com', 'Test123', 'empleado', 'Víctor', 'Mendoza', '2664-252626', 'activo', '34567890125', 'Chubut 3425, Villa Mercedes', NULL),
(309, 'test1@inmobiliaria01.com', 'Test123', 'empleado', 'Roberto', 'Martín', '2664-101010', 'activo', '30123456789', 'Av. Pringles 1001, San Luis', NULL),
(310, 'test2@inmobiliaria02.com', 'Test123', 'empleado', 'Sandra', 'González', '2664-202020', 'eliminado', '31234567890', 'Belgrano 1102, San Luis', NULL),
(311, 'test3@inmobiliaria03.com', 'Test123', 'empleado', 'Daniel', 'Fernández', '2664-303030', 'activo', '32345678901', 'Mitre 1203, Villa Mercedes', NULL),
(312, 'test4@inmobiliaria04.com', '$2a$11$V0OGMNYyMXj6PlGggRQjn.rBpzqSElereNDgUubQ5LuzjqdyI9zk.', 'empleado', 'Claudia', 'Vega', '2664-404040', 'activo', '33456789012', 'Sarmiento 1304, San Luis', NULL),
(313, 'test5@inmobiliaria05.com', 'Test123', 'empleado', 'Marcelo', 'Ríos', '2664-505050', 'activo', '34567890123', 'Rivadavia 1405, Villa Mercedes', NULL),
(314, 'test6@inmobiliaria06.com', 'Test123', 'empleado', 'Silvia', 'Cabrera', '2664-606060', 'activo', '35678901234', 'Colón 1506, San Luis', NULL),
(315, 'test7@inmobiliaria07.com', 'Test123', 'empleado', 'Gustavo', 'Moreno', '2664-707070', 'activo', '36789012345', 'Pringles 1607, Villa Mercedes', NULL),
(316, 'test8@inmobiliaria08.com', 'Test123', 'empleado', 'Liliana', 'Sosa', '2664-808080', 'activo', '37890123456', 'Junín 1708, San Luis', NULL),
(317, 'test9@inmobiliaria09.com', 'Test123', 'empleado', 'Adrián', 'Blanco', '2664-909090', 'activo', '38901234567', 'Chacabuco 1809, Villa Mercedes', NULL),
(318, 'test10@inmobiliaria10.com', 'Test123', 'empleado', 'Beatriz', 'Navarro', '2664-101111', 'activo', '39012345678', 'Independencia 1910, San Luis', NULL),
(319, 'test11@inmobiliaria11.com', 'Test123', 'empleado', 'Sergio', 'Paz', '2664-111212', 'activo', '30123456780', 'Libertad 2011, Villa Mercedes', NULL),
(320, 'test12@inmobiliaria12.com', 'Test123', 'empleado', 'Nora', 'Álvarez', '2664-121313', 'activo', '31234567891', 'Constitución 2112, San Luis', NULL),
(321, 'test13@inmobiliaria13.com', 'Test123', 'empleado', 'Rubén', 'Guerrero', '2664-131414', 'activo', '32345678902', 'Tucumán 2213, Villa Mercedes', NULL),
(322, 'test14@inmobiliaria14.com', 'Test123', 'empleado', 'Gloria', 'Romero', '2664-141515', 'activo', '33456789013', 'Córdoba 2314, San Luis', NULL),
(323, 'test15@inmobiliaria15.com', 'Test123', 'empleado', 'Oscar', 'Molina', '2664-151616', 'activo', '34567890124', 'Entre Ríos 2415, Villa Mercedes', NULL),
(324, 'test16@inmobiliaria16.com', 'Test123', 'empleado', 'Marta', 'Benítez', '2664-161717', 'activo', '35678901235', 'Santa Fe 2516, San Luis', NULL),
(325, 'test17@inmobiliaria17.com', 'Test123', 'empleado', 'Raúl', 'Campos', '2664-171818', 'activo', '36789012346', 'Mendoza 2617, Villa Mercedes', NULL),
(326, 'test18@inmobiliaria18.com', 'Test123', 'empleado', 'Cristina', 'Valdez', '2664-181919', 'activo', '37890123457', 'La Rioja 2718, San Luis', NULL),
(327, 'test19@inmobiliaria19.com', 'Test123', 'empleado', 'Eduardo', 'Rojas', '2664-192020', 'activo', '38901234568', 'Catamarca 2819, Villa Mercedes', NULL),
(328, 'test20@inmobiliaria20.com', 'Test123', 'empleado', 'Rosa', 'Figueroa', '2664-202121', 'activo', '39012345679', 'Jujuy 2920, San Luis', NULL),
(329, 'test21@inmobiliaria21.com', 'Test123', 'empleado', 'Pablo', 'Acosta', '2664-212222', 'activo', '30123456781', 'Salta 3021, Villa Mercedes', NULL),
(330, 'test22@inmobiliaria22.com', 'Test123', 'empleado', 'Elena', 'Cortés', '2664-222323', 'activo', '31234567892', 'Formosa 3122, San Luis', NULL),
(331, 'test23@inmobiliaria23.com', 'Test123', 'empleado', 'Héctor', 'Espinoza', '2664-232424', 'activo', '32345678903', 'Neuquén 3223, Villa Mercedes', NULL),
(332, 'test24@inmobiliaria24.com', 'Test123', 'empleado', 'Irma', 'Peña', '2664-242525', 'activo', '33456789014', 'Río Negro 3324, San Luis', NULL),
(333, 'test25@inmobiliaria25.com', 'Test123', 'empleado', 'Víctor', 'Mendoza', '2664-252626', 'activo', '34567890125', 'Chubut 3425, Villa Mercedes', NULL),
(335, 'sin1-email@dominio.com', '$2a$11$C6mZ5ft6eW4l7kKj5jqc9Ofa5QyM22nfNkLYqfEFS3xfr8kOh.BfC', 'propietario', 'Sin nombre1', 'Sin apellido1', '2658745456', 'inactivo', '00000001', 'Sin dirección especificada1', NULL),
(336, 'si2-email@dominio.com', '$2a$11$GzVrfgtSy4y8OZj6M.jw3.b3bqGxk4YCK6buO9UW7GMWmKksQhqtm', 'propietario', 'Sin nombre1', 'Sin apellido1', '265874587', 'inactivo', '00000101', 'Sin dirección especificada1', NULL),
(337, 'si11-email@dominio.com', '$2a$11$N6KC5lVwg3gNMRqAwgFSMOJPVXEc0fFAp0I2G6i38h.udA7YB6gVG', 'inquilino', 'ahora tiene nombre', 'Sin apellido1', '2658745456', 'activo', '00000456', 'Sin dirección especificada1', NULL),
(338, 'juan.perez@email.com', '', 'empleado', 'Juan', 'Pérez', '2664123456', 'inactivo', '12345678', 'Av. San Martín 123, Villa Mercedes', NULL),
(339, 'maria.gonzalez@email.com', '', 'empleado', 'María', 'González', '2664234567', 'inactivo', '23456789', 'Belgrano 456, Villa Mercedes', NULL),
(340, 'carlos.rodriguez@email.com', '', 'empleado', 'Carlos', 'Rodríguez', '2664345678', 'inactivo', '34567890', 'Mitre 789, Villa Mercedes', NULL),
(341, 'ana.lopez@email.com', '', 'empleado', 'Ana', 'López', '2664456789', 'inactivo', '45678901', 'Rivadavia 321, Villa Mercedes', NULL),
(342, 'pedro.martinez@email.com', '', 'empleado', 'Pedro', 'Martínez', '2664567890', 'inactivo', '56789012', 'Sarmiento 654, Villa Mercedes', NULL),
(343, 'laura.sanchez@email.com', '', 'empleado', 'Laura', 'Sánchez', '2664678901', 'inactivo', '67890123', 'Córdoba 987, Villa Mercedes', NULL),
(344, 'diego.fernandez@email.com', '', 'empleado', 'Diego', 'Fernández', '2664789012', 'inactivo', '78901234', 'Tucumán 147, Villa Mercedes', NULL),
(345, 'sofia.herrera@email.com', '', 'empleado', 'Sofía', 'Herrera', '2664890123', 'inactivo', '89012345', 'Entre Ríos 258, Villa Mercedes', NULL),
(346, 'martin.silva@email.com', '', 'empleado', 'Martín', 'Silva', '2664901234', 'inactivo', '90123456', 'Chacabuco 369, Villa Mercedes', NULL),
(347, 'valeria.torres@email.com', '', 'empleado', 'Valeria', 'Torres', '2664012345', 'inactivo', '01234567', 'San Juan 741, Villa Mercedes', NULL),
(368, 'juan1.perez@email.com', '', 'empleado', 'Juan', 'Pérezz', '2664123456', 'activo', '1234678', 'Av. San Martín 123, Villa Mercedes', NULL),
(369, 'mariaa.gonzalez@email.com', '', 'empleado', 'María', 'Gonzáleez', '2664234567', 'activo', '23446789', 'Belgrano 456, Villa Mercedes', NULL),
(370, 'carlosa.rodriguez@email.com', '', 'empleado', 'Carlos', 'Rodríguez', '2664347678', 'inactivo', '34467890', 'Mitre 789, Villa Mercedes', NULL),
(371, 'anaa.lopez@email.com', '', 'empleado', 'Ana', 'López', '2664456779', 'activo', '45478901', 'Rivadavia 321, Villa Mercedes', NULL),
(372, 'pedroa.martinez@email.com', '', 'empleado', 'Pedro', 'Martínez', '2667567890', 'activo', '56489012', 'Sarmiento 654, Villa Mercedes', NULL),
(373, 'lauraa.sanchez@email.com', '', 'empleado', 'Laura', 'Sánchez', '2664778901', 'activo', '67490123', 'Córdoba 987, Villa Mercedes', NULL),
(374, 'diegao.fernandez@email.com', '', 'empleado', 'Diego', 'Fernández', '2674789012', 'activo', '78401234', 'Tucumán 147, Villa Mercedes', NULL),
(375, 'sofiaa.herrera@email.com', '', 'empleado', 'Sofía', 'Herrera', '2664790123', 'activo', '89412345', 'Entre Ríos 258, Villa Mercedes', NULL),
(376, 'martian.silva@email.com', '', 'empleado', 'Martín', 'Silva', '2664971234', 'activo', '90423456', 'Chacabuco 369, Villa Mercedes', NULL),
(377, 'valeriaa.torres@email.com', '', 'empleado', 'Valeria', 'Torres', '2667012345', 'activo', '01434567', 'San Juan 741, Villa Mercedes', NULL),
(378, 'si121-email@dominio.com', '$2a$11$LP9pW4jZLse2MZ7KBWTUJOxs7RhmC50dB8zLVBDqTQUeiPr97AFmS', 'propietario', 'lisandro', 'buenas', '3245878564', 'inactivo', '245666987', 'calle numero y nose ', NULL),
(379, 'simoncito@gmail.com', '$2a$11$FpHOvDEbHRHe8nUlFpPctOUkIv2SiKe2hP03xOgyXvMIMXfVALTay', 'propietario', 'simon', 'bolivar', '2657645888', 'inactivo', '203154878', 'calle de simon', NULL),
(380, 'juan@gmail.com', '$2a$11$xgyfbzmSTXifD0OCDAbSb.bo1XUrsNoZAHygUaZ8C24exJiLzBCKC', 'propietario', 'juan', 'Barbaran', '2657848788', 'inactivo', '34987652', 'ciudad jardion ruta 7 ', NULL),
(381, 'peke@gmail.com', '$2a$11$69YRmaj/GYLSYE/uMsU.deprG6nvMoXtD0qee38.leXjadT1EcHQG', 'propietario', 'carlos', 'joseperkermen', '262626262', 'inactivo', '2657878546', 'las flores', NULL),
(382, 'inqui@gmail.com', '$2a$11$ooxVUnN2z1eQHR1ZSIkwu.C1BpRhAQcg.pAtbRnIIkCeD7vmD5Eoe', 'inquilino', 'inquilino', 'inquilino', '454878456', 'inactivo', '265774854', 'calle del inquilino', NULL),
(383, 'empleado@inmobiliaria.com', '$2a$11$vSdZCuvZoGWaUz4d5XMdPuvMGXryzcVOzI4jyuyPvcM5Lwaa0XbSq', 'empleado', 'Juan', 'Empleado', '2664222222', 'activo', '22222222', 'Dirección Empleado', 'https://via.placeholder.com/150/ffc107/000000?text=JE'),
(384, 'propietario@inmobiliaria.com', '$2a$11$yCTNNpJDx3wAWMIGqnxi1O5zuJFsmtpUrfFkahB8baoodX44ce3YG', 'propietario', 'María', 'Propietaria', '2664333333', 'activo', '33333333', 'Dirección Propietaria', 'https://via.placeholder.com/150/007bff/ffffff?text=MP'),
(385, 'inquilino@inmobiliaria.com', '$2a$11$OV21j3xs0cVpwFVEKjZsT.VxQydNE43TXM8awev9lCI7DpnujuTnC', 'inquilino', 'Pedro', 'Inquilino', '2664444444', 'activo', '44444444', 'Dirección Inquilino', 'https://via.placeholder.com/150/28a745/ffffff?text=PI'),
(386, 'admin@inmobiliaria.com', '$2a$11$hKoMVlUkLhKIyp6DbENHLOXX9AjGKhJWRFasrF8LFQXeF4og1DQMa', 'administrador', 'Super', 'Administrador', '2664111111', 'activo', '11111111', 'Dirección Admin', 'https://via.placeholder.com/150/dc3545/ffffff?text=SA'),
(387, 'CarlosAlberto@dominio.com', '$2a$11$ugguCWTZBoNnOyUIr018c.2r/gTttCahJCJq8DeLmFEzrOGYrkkeu', 'inquilino', 'CarlosAlberto', 'peñaloza', '265712345', 'inactivo', '256878962', 'La Rioja 2718, San Luis', 'https://via.placeholder.com/150/28a745/ffffff?text=CP'),
(388, 'jose@gmail.com', '$2a$11$LLYPbDcx0FrSiRBHggHPv.k6LwOxQZ9gLdLX0rVnlhycILg1oPrRe', 'propietario', 'joseAntoni', 'Sin apellido5', '2658478585', 'activo', '2658487855', 'calle viva ', 'https://via.placeholder.com/150/007bff/ffffff?text=JS'),
(389, 'pepito@gmail.com', '$2a$11$IN7ABXPG.wnBaR7S3ZwJ8.Ms2fQcIYrpclxqT/YqYXNVRFuWMgvay', 'propietario', 'pepitoJuan', 'dominguez', '265987552', 'inactivo', '458785452', 'calle de pepito', 'https://via.placeholder.com/150/007bff/ffffff?text=PD'),
(390, 'andrea@gmail.com', '$2a$11$lhmCxN.XBj/hviJsnOrWx.jogts.w9YaJMB7nMSGz06BtNGe7yPwi', 'propietario', 'AndreaMagaly', 'Tobare', '2365548785', 'activo', '31485247', 'calle de andrea', 'https://img.freepik.com/vector-gratis/nina-feliz-mariposa_1450-103.jpg?semt=ais_hybrid&w=740&q=80'),
(391, 'Carlos@gmail.com', '$2a$11$iKFtlCHLliTbr2.pcPOtVuD3sPjk66dVi2gV2vPUWU/Wi5/tzelVq', 'inquilino', 'Carlos miguel', 'Soria', '452875568', 'inactivo', '245875214', 'calle de carlos', 'https://via.placeholder.com/150/28a745/ffffff?text=CS'),
(392, 'leandrosebastiantroncoso@gmail.com', '$2a$11$5aubB0BrfgOzqCmfQjABN.pupOjr0xd29Akh9Byqp.ZK8plotsNkC', 'propietario', 'Leandro Sebastian', 'Tobare', '267548562', 'activo', '34966671', 'casa de leandro', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSc1fT0QdG-Vj25ZcEfGo4z4ZHygyFotPpo2w&s'),
(393, 'leandro@gmail.com', '$2a$11$bBPz8OEk756k7UR4rsTDH.dvCgtRiUK5E0Qo6C1c7ydxOEcofgvlG', 'propietario', 'Leandro Sebastian', 'Blanco', '2657645467', 'activo', '3496667 ', 'calle pensador', '/avatars/avatar_9085f21d-6772-463f-ba88-df6a30c69b1c.png'),
(394, 'leandroseoncoso@gmail.com', '$2a$11$N/D2huXr9PHYNmjhaeSpS.cbFsxolzhbA3XCOBK3HAvHeEzND8NZm', 'propietario', 'leandro javier nose', 'palacios', '26576454668', 'activo', '34966672', 'calle de palacios', 'https://via.placeholder.com/150/007bff/ffffff?text=LP'),
(395, 'leandrosebastian@gmail.com', '$2a$11$RuPv5irY7z3rn6e/GIplI.GGGpFYb3mPPiwfs2ENAKeBcTwp4C4uO', 'propietario', 'leandro', 'Navarro', '2657645465', 'activo', '34966673', 'casa de navarro', 'https://via.placeholder.com/150/007bff/ffffff?text=LN'),
(396, 'leandrosebastiantrso@gmail.com', '$2a$11$rvVGgnOiLoSsw/RFYZA0We8ho0Oyfnhbkh/nedKqKqFi0wjqlBGVK', 'propietario', 'leandro jose', 'comas', '2657648785', 'activo', '34966676', 'Casa de leadnro', 'https://via.placeholder.com/150/007bff/ffffff?text=LC'),
(397, 'leandrosebastiancoso@gmail.com', '$2a$11$TzJVoDPt3uW05HmUhmo/TulxXf.5cOHDpI1PxQegwDCmQdoDy7l7q', 'propietario', 'Leandro Sebastian sas', 'Blancoso', '2657645469', 'activo', '34966678', 'casas de el ', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPkxCPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(398, 'pedro.martinezjuan@email.com', '$2a$11$oqcGrxJY9Dn55L2yoqbbuuY2QAkeybA3yMVg8Nqf3oKepNMqqA/BG', 'propietario', 'JuanCarlosa', 'Pérez', '2658745457', 'activo', '34966675', 'calle de juan', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPkpQPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(399, 'leandrosebastianTronco@gmail.com', '$2a$11$J9228hflHsyWw/AGUsSqh.lDMJ89/OlNv7I4cCVzEvEoZdHZiv0oq', 'inquilino', 'Sin nombreInquilino', 'Sin apellido1', '2658745458', 'activo', '34966679', 'calle sin nombre', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMjhhNzQ1Jy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPlNTPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(400, 'claudio@gmail.com', '$2a$11$2Ky.CsaO1f7ltcQaI2cbz.cB84sGB689WpJ.6lMdMItOsoocbzedK', 'inquilino', 'Claudio ', 'Gonzalez', '4548875421', 'activo', '12485248', 'ruta 7 km 698 barrio ciudad jardin', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMjhhNzQ1Jy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPkNHPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(401, 'laura@gmail.com', '$2a$11$lyCoDmHwrvqKflwEBkclg.tR8UmfMZCK6lD.N5W50caf2I.1cqRfa', 'propietario', 'Laura', 'Troncoso', '232659865', 'activo', '35598956', 'tucuman ', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPkxUPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(402, 'jonatan@gmail.com', '$2a$11$poddJSos59yTRKymc09t/uiVNA.wIoVpVeNv3ZpvdKlUgvzULzOJi', 'propietario', 'jonatan', 'Gomez', '115151131', 'activo', '4582215', 'pringles 20', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPkpHPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(403, 'isaias@gmail.com', '$2a$11$WtsjIVuXatN1D8WiwyYr.eSSNRNp4LCpfAOk85w4OwUHLBLkyJZxK', 'propietario', 'isaias', 'larea', '23685521', 'activo', '2658725', 'calle balcarce ', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPklMPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(404, 'agos@gmail.com', '$2a$11$WvLYQEY3RUDtQfV9hhZRN.TZPhLbfes5DNC8iZIM7ZSk3r3B1S77y', 'propietario', ' agostina ', 'cabanez', '2657232323', 'activo', '50222200', 'tucuman 90', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPiBDPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(405, 'bella@gmail.com', '$2a$11$hBfwsMoFW2Rga2J7IcQnjuif5Qk2LaULcZ0SHsZisqFLqvfpAiAQS', 'propietario', 'shania', 'aguero', '2657114477', 'activo', '56000222', 'margarita 111', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMDA3YmZmJy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPlNBPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4='),
(406, 'sack@gmail.com', '$2a$11$nM2SQKkggn3PATkCpV9Uu.iKqgYKKc7vGbOweGfftpeTOfXdhHe4m', 'inquilino', 'isaac', 'oro', '2657889911', 'activo', '55000333', 'la rosa 16', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPScxNTAnIGhlaWdodD0nMTUwJyB2aWV3Qm94PScwIDAgMTUwIDE1MCc+DQogICAgICAgIDxjaXJjbGUgY3g9Jzc1JyBjeT0nNzUnIHI9Jzc1JyBmaWxsPScjMjhhNzQ1Jy8+DQogICAgICAgIDx0ZXh0IHg9JzUwJScgeT0nNTAlJyBmb250LWZhbWlseT0nQXJpYWwsIHNhbnMtc2VyaWYnIGZvbnQtc2l6ZT0nNjAnIGZvbnQtd2VpZ2h0PSdib2xkJyANCiAgICAgICAgICAgICAgZmlsbD0nI2ZmZmZmZicgdGV4dC1hbmNob3I9J21pZGRsZScgZG9taW5hbnQtYmFzZWxpbmU9J2NlbnRyYWwnPklPPC90ZXh0Pg0KICAgICAgICA8L3N2Zz4=');

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `contacto`
--
ALTER TABLE `contacto`
  ADD PRIMARY KEY (`id_contacto`),
  ADD KEY `idx_fecha_contacto` (`fecha_contacto`),
  ADD KEY `idx_estado` (`estado`),
  ADD KEY `idx_email` (`email`);

--
-- Indices de la tabla `contrato`
--
ALTER TABLE `contrato`
  ADD PRIMARY KEY (`id_contrato`),
  ADD KEY `id_usuario_creador` (`id_usuario_creador`),
  ADD KEY `id_usuario_terminador` (`id_usuario_terminador`),
  ADD KEY `idx_contrato_inmueble` (`id_inmueble`),
  ADD KEY `idx_contrato_inquilino` (`id_inquilino`),
  ADD KEY `idx_contrato_fechas` (`fecha_inicio`,`fecha_fin`),
  ADD KEY `fk_contrato_propietario` (`id_propietario`);

--
-- Indices de la tabla `contrato_venta`
--
ALTER TABLE `contrato_venta`
  ADD PRIMARY KEY (`id_contrato_venta`),
  ADD KEY `id_usuario_creador` (`id_usuario_creador`),
  ADD KEY `id_usuario_cancelador` (`id_usuario_cancelador`),
  ADD KEY `idx_estado` (`estado`),
  ADD KEY `idx_inmueble` (`id_inmueble`),
  ADD KEY `idx_comprador` (`id_comprador`),
  ADD KEY `idx_vendedor` (`id_vendedor`);

--
-- Indices de la tabla `imagen_inmueble`
--
ALTER TABLE `imagen_inmueble`
  ADD PRIMARY KEY (`id_imagen`),
  ADD KEY `id_inmueble` (`id_inmueble`);

--
-- Indices de la tabla `inmueble`
--
ALTER TABLE `inmueble`
  ADD PRIMARY KEY (`id_inmueble`),
  ADD KEY `idx_inmueble_propietario` (`id_propietario`),
  ADD KEY `idx_inmueble_tipo` (`id_tipo_inmueble`);

--
-- Indices de la tabla `inquilino`
--
ALTER TABLE `inquilino`
  ADD PRIMARY KEY (`id_inquilino`),
  ADD KEY `id_usuario` (`id_usuario`);

--
-- Indices de la tabla `interes_inmueble`
--
ALTER TABLE `interes_inmueble`
  ADD PRIMARY KEY (`id_interes`),
  ADD KEY `id_inmueble` (`id_inmueble`);

--
-- Indices de la tabla `pago`
--
ALTER TABLE `pago`
  ADD PRIMARY KEY (`id_pago`),
  ADD KEY `id_usuario_creador` (`id_usuario_creador`),
  ADD KEY `id_usuario_anulador` (`id_usuario_anulador`),
  ADD KEY `idx_pago_contrato` (`id_contrato`),
  ADD KEY `idx_pago_fecha` (`fecha_pago`),
  ADD KEY `idx_pago_inmueble` (`id_inmueble`),
  ADD KEY `idx_periodo` (`id_contrato`,`periodo_año`,`periodo_mes`);

--
-- Indices de la tabla `propietario`
--
ALTER TABLE `propietario`
  ADD PRIMARY KEY (`id_propietario`),
  ADD KEY `id_usuario` (`id_usuario`);

--
-- Indices de la tabla `tipo_inmueble`
--
ALTER TABLE `tipo_inmueble`
  ADD PRIMARY KEY (`id_tipo_inmueble`);

--
-- Indices de la tabla `usuario`
--
ALTER TABLE `usuario`
  ADD PRIMARY KEY (`id_usuario`),
  ADD UNIQUE KEY `email` (`email`);

--
-- AUTO_INCREMENT de las tablas volcadas
--

--
-- AUTO_INCREMENT de la tabla `contacto`
--
ALTER TABLE `contacto`
  MODIFY `id_contacto` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `contrato`
--
ALTER TABLE `contrato`
  MODIFY `id_contrato` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `contrato_venta`
--
ALTER TABLE `contrato_venta`
  MODIFY `id_contrato_venta` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=10;

--
-- AUTO_INCREMENT de la tabla `imagen_inmueble`
--
ALTER TABLE `imagen_inmueble`
  MODIFY `id_imagen` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=42;

--
-- AUTO_INCREMENT de la tabla `inmueble`
--
ALTER TABLE `inmueble`
  MODIFY `id_inmueble` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=83;

--
-- AUTO_INCREMENT de la tabla `inquilino`
--
ALTER TABLE `inquilino`
  MODIFY `id_inquilino` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=73;

--
-- AUTO_INCREMENT de la tabla `interes_inmueble`
--
ALTER TABLE `interes_inmueble`
  MODIFY `id_interes` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT de la tabla `pago`
--
ALTER TABLE `pago`
  MODIFY `id_pago` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=32;

--
-- AUTO_INCREMENT de la tabla `propietario`
--
ALTER TABLE `propietario`
  MODIFY `id_propietario` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=76;

--
-- AUTO_INCREMENT de la tabla `tipo_inmueble`
--
ALTER TABLE `tipo_inmueble`
  MODIFY `id_tipo_inmueble` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=52;

--
-- AUTO_INCREMENT de la tabla `usuario`
--
ALTER TABLE `usuario`
  MODIFY `id_usuario` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=407;

--
-- Restricciones para tablas volcadas
--

--
-- Filtros para la tabla `contrato`
--
ALTER TABLE `contrato`
  ADD CONSTRAINT `contrato_ibfk_1` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`),
  ADD CONSTRAINT `contrato_ibfk_2` FOREIGN KEY (`id_inquilino`) REFERENCES `inquilino` (`id_inquilino`),
  ADD CONSTRAINT `contrato_ibfk_3` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuario` (`id_usuario`),
  ADD CONSTRAINT `contrato_ibfk_4` FOREIGN KEY (`id_usuario_terminador`) REFERENCES `usuario` (`id_usuario`),
  ADD CONSTRAINT `fk_contrato_propietario` FOREIGN KEY (`id_propietario`) REFERENCES `propietario` (`id_propietario`);

--
-- Filtros para la tabla `contrato_venta`
--
ALTER TABLE `contrato_venta`
  ADD CONSTRAINT `contrato_venta_ibfk_1` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`),
  ADD CONSTRAINT `contrato_venta_ibfk_2` FOREIGN KEY (`id_comprador`) REFERENCES `usuario` (`id_usuario`),
  ADD CONSTRAINT `contrato_venta_ibfk_3` FOREIGN KEY (`id_vendedor`) REFERENCES `propietario` (`id_propietario`),
  ADD CONSTRAINT `contrato_venta_ibfk_4` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuario` (`id_usuario`),
  ADD CONSTRAINT `contrato_venta_ibfk_5` FOREIGN KEY (`id_usuario_cancelador`) REFERENCES `usuario` (`id_usuario`);

--
-- Filtros para la tabla `imagen_inmueble`
--
ALTER TABLE `imagen_inmueble`
  ADD CONSTRAINT `imagen_inmueble_ibfk_1` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`);

--
-- Filtros para la tabla `inmueble`
--
ALTER TABLE `inmueble`
  ADD CONSTRAINT `inmueble_ibfk_1` FOREIGN KEY (`id_propietario`) REFERENCES `propietario` (`id_propietario`),
  ADD CONSTRAINT `inmueble_ibfk_2` FOREIGN KEY (`id_tipo_inmueble`) REFERENCES `tipo_inmueble` (`id_tipo_inmueble`);

--
-- Filtros para la tabla `inquilino`
--
ALTER TABLE `inquilino`
  ADD CONSTRAINT `inquilino_ibfk_1` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`);

--
-- Filtros para la tabla `interes_inmueble`
--
ALTER TABLE `interes_inmueble`
  ADD CONSTRAINT `interes_inmueble_ibfk_1` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`);

--
-- Filtros para la tabla `pago`
--
ALTER TABLE `pago`
  ADD CONSTRAINT `fk_pago_inmueble` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`),
  ADD CONSTRAINT `pago_ibfk_1` FOREIGN KEY (`id_contrato`) REFERENCES `contrato` (`id_contrato`),
  ADD CONSTRAINT `pago_ibfk_2` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuario` (`id_usuario`),
  ADD CONSTRAINT `pago_ibfk_3` FOREIGN KEY (`id_usuario_anulador`) REFERENCES `usuario` (`id_usuario`);

--
-- Filtros para la tabla `propietario`
--
ALTER TABLE `propietario`
  ADD CONSTRAINT `propietario_ibfk_1` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
