-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1
-- Tiempo de generación: 22-08-2025 a las 20:42:46
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
-- Base de datos: `inmobiliaria_db`
--

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inquilinos`
--

CREATE TABLE `inquilinos` (
  `Id` int(11) NOT NULL,
  `Dni` varchar(20) NOT NULL,
  `Apellido` varchar(100) NOT NULL,
  `Nombre` varchar(100) NOT NULL,
  `Telefono` varchar(20) DEFAULT NULL,
  `Email` varchar(100) DEFAULT NULL,
  `FechaCreacion` datetime DEFAULT current_timestamp(),
  `Activo` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inquilinos`
--

INSERT INTO `inquilinos` (`Id`, `Dni`, `Apellido`, `Nombre`, `Telefono`, `Email`, `FechaCreacion`, `Activo`) VALUES
(1, '12345678', 'García', 'Juan', '1155551234', 'juan.garcia@email.com', '2023-01-15 00:00:00', 1),
(2, '23456789', 'López', 'María', '1166665678', 'maria.lopez@email.com', '2023-03-20 00:00:00', 1),
(3, '34567890', 'Rodríguez', 'Carlos', '1177779012', 'carlos.rodriguez@email.com', '2023-06-10 00:00:00', 0),
(4, '45678901', 'Pérez', 'Ana', '1188883456', 'ana.perez@email.com', '2023-09-05 00:00:00', 1),
(5, '56789012', 'Sánchez', 'Pedro', '1199997890', 'pedro.sanchez@email.com', '2023-12-01 00:00:00', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `propietarios`
--

CREATE TABLE `propietarios` (
  `Id` int(11) NOT NULL,
  `Dni` varchar(20) NOT NULL,
  `Apellido` varchar(100) NOT NULL,
  `Nombre` varchar(100) NOT NULL,
  `Telefono` varchar(20) DEFAULT NULL,
  `Email` varchar(100) DEFAULT NULL,
  `FechaCreacion` datetime DEFAULT current_timestamp(),
  `Activo` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `propietarios`
--

INSERT INTO `propietarios` (`Id`, `Dni`, `Apellido`, `Nombre`, `Telefono`, `Email`, `FechaCreacion`, `Activo`) VALUES
(1, '98765432', 'Martínez', 'Laura', '1144445678', 'laura.martinez@email.com', '2023-02-10 00:00:00', 1),
(2, '87654321', 'Fernández', 'José', '1133339012', 'jose.fernandez@email.com', '2023-04-15 00:00:00', 1),
(3, '76543210', 'González', 'Sofía', '1122223456', 'sofia.gonzalez@email.com', '2023-07-20 00:00:00', 0),
(4, '65432109', 'Ramírez', 'Luis', '1111117890', 'luis.ramirez@email.com', '2023-10-25 00:00:00', 1),
(5, '54321098', 'Díaz', 'Elena', '1100001234', 'elena.diaz@email.com', '2023-11-30 00:00:00', 1);

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `inquilinos`
--
ALTER TABLE `inquilinos`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `Dni` (`Dni`);

--
-- Indices de la tabla `propietarios`
--
ALTER TABLE `propietarios`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `Dni` (`Dni`);

--
-- AUTO_INCREMENT de las tablas volcadas
--

--
-- AUTO_INCREMENT de la tabla `inquilinos`
--
ALTER TABLE `inquilinos`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT de la tabla `propietarios`
--
ALTER TABLE `propietarios`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
