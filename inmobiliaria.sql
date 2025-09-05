-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1
-- Tiempo de generación: 28-08-2025 a las 21:01:25
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
-- Estructura de tabla para la tabla `contrato`
--

CREATE TABLE `contrato` (
  `id_contrato` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `id_inquilino` int(11) NOT NULL,
  `fecha_inicio` date NOT NULL,
  `fecha_fin` date NOT NULL,
  `fecha_fin_anticipada` date DEFAULT NULL,
  `monto_mensual` decimal(12,2) NOT NULL,
  `estado` enum('vigente','finalizado','finalizado_anticipado') DEFAULT 'vigente',
  `multa_aplicada` decimal(12,2) DEFAULT 0.00,
  `id_usuario_creador` int(11) NOT NULL,
  `id_usuario_terminador` int(11) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `fecha_modificacion` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ;

--
-- Volcado de datos para la tabla `contrato`
--

INSERT INTO `contrato` (`id_contrato`, `id_inmueble`, `id_inquilino`, `fecha_inicio`, `fecha_fin`, `fecha_fin_anticipada`, `monto_mensual`, `estado`, `multa_aplicada`, `id_usuario_creador`, `id_usuario_terminador`, `fecha_creacion`, `fecha_modificacion`) VALUES
(1, 1, 1, '2024-01-01', '2024-12-31', NULL, 50000.00, 'vigente', 0.00, 1, NULL, '2025-08-22 19:19:05', '2025-08-22 19:19:05'),
(2, 2, 2, '2024-02-01', '2024-11-30', NULL, 80000.00, 'vigente', 0.00, 2, NULL, '2025-08-22 19:19:05', '2025-08-22 19:19:05');

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
  `estado` enum('disponible','no_disponible','alquilado') DEFAULT 'disponible',
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inmueble`
--

INSERT INTO `inmueble` (`id_inmueble`, `id_propietario`, `id_tipo_inmueble`, `direccion`, `uso`, `ambientes`, `precio`, `coordenadas`, `estado`, `fecha_alta`) VALUES
(1, 1, 1, 'Av. Siempre Viva 742', 'residencial', 3, 50000.00, '-34.6037, -58.3816', 'disponible', '2025-08-22 19:19:05'),
(2, 2, 3, 'Calle Comercial 123', 'comercial', 1, 80000.00, '-34.6083, -58.3712', 'disponible', '2025-08-22 19:19:05');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inquilino`
--

CREATE TABLE `inquilino` (
  `id_inquilino` int(11) NOT NULL,
  `dni` varchar(20) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `direccion` varchar(255) DEFAULT NULL,
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inquilino`
--

INSERT INTO `inquilino` (`id_inquilino`, `dni`, `apellido`, `nombre`, `telefono`, `email`, `direccion`, `fecha_alta`, `estado`) VALUES
(1, '40123456', 'Martínez', 'Luis', '777888999', 'luis@martinez.com', NULL, '2025-08-22 19:19:05', 1),
(2, '40234567', 'Rodríguez', 'Sofía', '000111222', 'sofia@rodriguez.com', NULL, '2025-08-22 19:19:05', 1),
(3, '34966673', 'Troncoso ', 'Carlos', '2657645466', 'leandrosebastiantroncoso@gmail.com', 'assasasas', '2025-08-24 03:25:09', 1),
(4, '30123456', 'asdsadasd', 'asdsad', '2657645466', 'leandro@gmail.com', 'assasasas', '2025-08-24 03:27:09', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `pago`
--

CREATE TABLE `pago` (
  `id_pago` int(11) NOT NULL,
  `id_contrato` int(11) NOT NULL,
  `numero_pago` int(11) NOT NULL,
  `fecha_pago` date NOT NULL,
  `concepto` varchar(100) NOT NULL,
  `monto` decimal(12,2) NOT NULL,
  `estado` enum('activo','anulado') DEFAULT 'activo',
  `id_usuario_creador` int(11) NOT NULL,
  `id_usuario_anulador` int(11) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `fecha_anulacion` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `pago`
--

INSERT INTO `pago` (`id_pago`, `id_contrato`, `numero_pago`, `fecha_pago`, `concepto`, `monto`, `estado`, `id_usuario_creador`, `id_usuario_anulador`, `fecha_creacion`, `fecha_anulacion`) VALUES
(1, 1, 1, '2024-01-05', 'Mes enero', 50000.00, 'activo', 1, NULL, '2025-08-22 19:19:05', NULL),
(2, 1, 2, '2024-02-05', 'Mes febrero', 50000.00, 'activo', 1, NULL, '2025-08-22 19:19:05', NULL),
(3, 2, 1, '2024-02-10', 'Mes febrero', 80000.00, 'activo', 2, NULL, '2025-08-22 19:19:05', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `propietario`
--

CREATE TABLE `propietario` (
  `id_propietario` int(11) NOT NULL,
  `dni` varchar(20) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `direccion` varchar(255) DEFAULT NULL,
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `propietario`
--

INSERT INTO `propietario` (`id_propietario`, `dni`, `apellido`, `nombre`, `telefono`, `email`, `direccion`, `fecha_alta`, `estado`) VALUES
(1, '30123456', 'García', 'Carlos', '111222333', 'carlos@garcia.com', NULL, '2025-08-22 19:19:05', 1),
(2, '30234567', 'López', 'Ana', '444555666', 'ana@lopez.com', NULL, '2025-08-22 19:19:05', 1),
(3, '34966673', 'Troncoso ', 'Leandro Sebastian', '2657645466', 'leandro@gmail.com', 'assasasas', '2025-08-24 03:25:32', 1),
(4, '2123123123', 'asdasd', 'asdsad', '2657645466', 'leandro@gmail.com', 'asdasdsa', '2025-08-24 03:27:33', 0),
(5, '12313213', 'asdasdas', 'Carlos', '2657645466', 'leandro@gmail.com', 'asdasd', '2025-08-24 03:31:09', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `tipo_inmueble`
--

CREATE TABLE `tipo_inmueble` (
  `id_tipo_inmueble` int(11) NOT NULL,
  `nombre` varchar(50) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `tipo_inmueble`
--

INSERT INTO `tipo_inmueble` (`id_tipo_inmueble`, `nombre`, `descripcion`, `fecha_creacion`) VALUES
(1, 'Departamento', 'Unidad residencial en edificio', '2025-08-22 19:19:05'),
(2, 'Casa', 'Vivienda unifamiliar', '2025-08-22 19:19:05'),
(3, 'Local', 'Espacio comercial', '2025-08-22 19:19:05'),
(4, 'Depósito', 'Almacén o bodega', '2025-08-22 19:19:05'),
(5, 'Oficina', 'Espacio de trabajo comercial', '2025-08-22 19:19:05');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `usuario`
--

CREATE TABLE `usuario` (
  `id_usuario` int(11) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password` varchar(255) NOT NULL,
  `rol` enum('administrador','empleado') NOT NULL DEFAULT 'empleado',
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `avatar` varchar(255) DEFAULT NULL,
  `estado` enum('activo','inactivo') DEFAULT 'activo',
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `usuario`
--

INSERT INTO `usuario` (`id_usuario`, `email`, `password`, `rol`, `nombre`, `apellido`, `telefono`, `avatar`, `estado`, `fecha_creacion`) VALUES
(1, 'admin@inmobiliaria.com', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'administrador', 'Juan', 'Pérez', '123456789', NULL, 'activo', '2025-08-22 19:19:05'),
(2, 'empleado1@inmobiliaria.com', 'ccc13e8ab0819e3ab61719de4071ecae6c1d3cd35dc48b91cad3481f20922f9f', 'empleado', 'María', 'Gómez', '987654321', NULL, 'activo', '2025-08-22 19:19:05');

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `contrato`
--
ALTER TABLE `contrato`
  ADD PRIMARY KEY (`id_contrato`),
  ADD KEY `id_usuario_creador` (`id_usuario_creador`),
  ADD KEY `id_usuario_terminador` (`id_usuario_terminador`),
  ADD KEY `idx_contrato_inmueble` (`id_inmueble`),
  ADD KEY `idx_contrato_inquilino` (`id_inquilino`),
  ADD KEY `idx_contrato_fechas` (`fecha_inicio`,`fecha_fin`);

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
  ADD UNIQUE KEY `dni` (`dni`);

--
-- Indices de la tabla `pago`
--
ALTER TABLE `pago`
  ADD PRIMARY KEY (`id_pago`),
  ADD KEY `id_usuario_creador` (`id_usuario_creador`),
  ADD KEY `id_usuario_anulador` (`id_usuario_anulador`),
  ADD KEY `idx_pago_contrato` (`id_contrato`),
  ADD KEY `idx_pago_fecha` (`fecha_pago`);

--
-- Indices de la tabla `propietario`
--
ALTER TABLE `propietario`
  ADD PRIMARY KEY (`id_propietario`),
  ADD UNIQUE KEY `dni` (`dni`);

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
-- AUTO_INCREMENT de la tabla `contrato`
--
ALTER TABLE `contrato`
  MODIFY `id_contrato` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `inmueble`
--
ALTER TABLE `inmueble`
  MODIFY `id_inmueble` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `inquilino`
--
ALTER TABLE `inquilino`
  MODIFY `id_inquilino` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT de la tabla `pago`
--
ALTER TABLE `pago`
  MODIFY `id_pago` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT de la tabla `propietario`
--
ALTER TABLE `propietario`
  MODIFY `id_propietario` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT de la tabla `tipo_inmueble`
--
ALTER TABLE `tipo_inmueble`
  MODIFY `id_tipo_inmueble` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT de la tabla `usuario`
--
ALTER TABLE `usuario`
  MODIFY `id_usuario` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

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
  ADD CONSTRAINT `contrato_ibfk_4` FOREIGN KEY (`id_usuario_terminador`) REFERENCES `usuario` (`id_usuario`);

--
-- Filtros para la tabla `inmueble`
--
ALTER TABLE `inmueble`
  ADD CONSTRAINT `inmueble_ibfk_1` FOREIGN KEY (`id_propietario`) REFERENCES `propietario` (`id_propietario`),
  ADD CONSTRAINT `inmueble_ibfk_2` FOREIGN KEY (`id_tipo_inmueble`) REFERENCES `tipo_inmueble` (`id_tipo_inmueble`);

--
-- Filtros para la tabla `pago`
--
ALTER TABLE `pago`
  ADD CONSTRAINT `pago_ibfk_1` FOREIGN KEY (`id_contrato`) REFERENCES `contrato` (`id_contrato`),
  ADD CONSTRAINT `pago_ibfk_2` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuario` (`id_usuario`),
  ADD CONSTRAINT `pago_ibfk_3` FOREIGN KEY (`id_usuario_anulador`) REFERENCES `usuario` (`id_usuario`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
