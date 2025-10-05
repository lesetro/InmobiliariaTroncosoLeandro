-- ==============================================
-- BASE DE DATOS INMOBILIARIA - ESTRUCTURA COMPLETA
-- Todas las tablas con sus columnas y tipos de datos correctos
-- Sin datos precargados
-- ==============================================

-- Crear base de datos
CREATE DATABASE IF NOT EXISTS `inmobiliaria` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `inmobiliaria`;

-- Deshabilitar verificaciones temporalmente
SET FOREIGN_KEY_CHECKS = 0;

-- ==============================================
-- 1. TABLA USUARIO - Tabla principal de usuarios
-- ==============================================
CREATE TABLE `usuario` (
  `id_usuario` int(11) NOT NULL AUTO_INCREMENT,
  `email` varchar(100) NOT NULL,
  `password` varchar(255) NOT NULL,
  `rol` varchar(20) NOT NULL DEFAULT 'empleado',
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `telefono` varchar(20) NOT NULL,
  `estado` varchar(20) NOT NULL DEFAULT 'activo',
  `dni` varchar(20) NOT NULL,
  `direccion` varchar(255) NOT NULL,
  `avatar` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`id_usuario`),
  UNIQUE KEY `email` (`email`),
  KEY `idx_usuario_rol` (`rol`),
  KEY `idx_usuario_estado` (`estado`),
  KEY `idx_usuario_email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 2. TABLA TIPO_INMUEBLE - Tipos de propiedades
-- ==============================================
CREATE TABLE `tipo_inmueble` (
  `id_tipo_inmueble` int(11) NOT NULL AUTO_INCREMENT,
  `nombre` varchar(50) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id_tipo_inmueble`),
  KEY `idx_tipo_inmueble_estado` (`estado`),
  KEY `idx_tipo_inmueble_nombre` (`nombre`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 3. TABLA PROPIETARIO - Propietarios de inmuebles
-- ==============================================
CREATE TABLE `propietario` (
  `id_propietario` int(11) NOT NULL AUTO_INCREMENT,
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1,
  `id_usuario` int(11) NOT NULL,
  PRIMARY KEY (`id_propietario`),
  KEY `idx_propietario_usuario` (`id_usuario`),
  KEY `idx_propietario_estado` (`estado`),
  CONSTRAINT `fk_propietario_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 4. TABLA INQUILINO - Inquilinos de propiedades
-- ==============================================
CREATE TABLE `inquilino` (
  `id_inquilino` int(11) NOT NULL AUTO_INCREMENT,
  `fecha_alta` timestamp NOT NULL DEFAULT current_timestamp(),
  `estado` tinyint(1) DEFAULT 1,
  `id_usuario` int(11) NOT NULL,
  PRIMARY KEY (`id_inquilino`),
  KEY `idx_inquilino_usuario` (`id_usuario`),
  KEY `idx_inquilino_estado` (`estado`),
  CONSTRAINT `fk_inquilino_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 5. TABLA INMUEBLE - Propiedades inmobiliarias
-- ==============================================
CREATE TABLE `inmueble` (
  `id_inmueble` int(11) NOT NULL AUTO_INCREMENT,
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
  PRIMARY KEY (`id_inmueble`),
  KEY `idx_inmueble_propietario` (`id_propietario`),
  KEY `idx_inmueble_tipo` (`id_tipo_inmueble`),
  KEY `idx_inmueble_estado` (`estado`),
  KEY `idx_inmueble_uso` (`uso`),
  KEY `idx_inmueble_precio` (`precio`),
  CONSTRAINT `fk_inmueble_propietario` FOREIGN KEY (`id_propietario`) REFERENCES `propietario` (`id_propietario`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_inmueble_tipo` FOREIGN KEY (`id_tipo_inmueble`) REFERENCES `tipo_inmueble` (`id_tipo_inmueble`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 6. TABLA CONTRATO - Contratos de alquiler/venta
-- ==============================================
CREATE TABLE `contrato` (
  `id_contrato` int(11) NOT NULL AUTO_INCREMENT,
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
  PRIMARY KEY (`id_contrato`),
  KEY `idx_contrato_inmueble` (`id_inmueble`),
  KEY `idx_contrato_inquilino` (`id_inquilino`),
  KEY `idx_contrato_propietario` (`id_propietario`),
  KEY `idx_contrato_usuario_creador` (`id_usuario_creador`),
  KEY `idx_contrato_usuario_terminador` (`id_usuario_terminador`),
  KEY `idx_contrato_fechas` (`fecha_inicio`,`fecha_fin`),
  KEY `idx_contrato_estado` (`estado`),
  CONSTRAINT `fk_contrato_inmueble` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_contrato_inquilino` FOREIGN KEY (`id_inquilino`) REFERENCES `inquilino` (`id_inquilino`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_contrato_propietario` FOREIGN KEY (`id_propietario`) REFERENCES `propietario` (`id_propietario`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_contrato_usuario_creador` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuario` (`id_usuario`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_contrato_usuario_terminador` FOREIGN KEY (`id_usuario_terminador`) REFERENCES `usuario` (`id_usuario`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 7. TABLA PAGO - Pagos de alquileres
-- ==============================================
CREATE TABLE `pago` (
  `id_pago` int(11) NOT NULL AUTO_INCREMENT,
  `id_contrato` int(11) NOT NULL,
  `numero_pago` int(11) NOT NULL,
  `fecha_pago` date NOT NULL,
  `concepto` varchar(100) NOT NULL,
  `monto` decimal(12,2) NOT NULL,
  `estado` enum('activo','anulado') DEFAULT 'activo',
  `id_usuario_creador` int(11) NOT NULL,
  `id_usuario_anulador` int(11) DEFAULT NULL,
  `fecha_creacion` timestamp NOT NULL DEFAULT current_timestamp(),
  `fecha_anulacion` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id_pago`),
  KEY `idx_pago_contrato` (`id_contrato`),
  KEY `idx_pago_fecha` (`fecha_pago`),
  KEY `idx_pago_estado` (`estado`),
  KEY `idx_pago_usuario_creador` (`id_usuario_creador`),
  KEY `idx_pago_usuario_anulador` (`id_usuario_anulador`),
  KEY `idx_pago_numero` (`numero_pago`),
  CONSTRAINT `fk_pago_contrato` FOREIGN KEY (`id_contrato`) REFERENCES `contrato` (`id_contrato`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_pago_usuario_creador` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuario` (`id_usuario`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_pago_usuario_anulador` FOREIGN KEY (`id_usuario_anulador`) REFERENCES `usuario` (`id_usuario`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 8. TABLA INTERES_INMUEBLE - Personas interesadas
-- ==============================================
CREATE TABLE `interes_inmueble` (
  `id_interes` int(11) NOT NULL AUTO_INCREMENT,
  `id_inmueble` int(11) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `email` varchar(100) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `fecha` datetime DEFAULT current_timestamp(),
  `contactado` tinyint(1) DEFAULT 0,
  `fecha_contacto` datetime DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  PRIMARY KEY (`id_interes`),
  KEY `idx_interes_inmueble` (`id_inmueble`),
  KEY `idx_interes_fecha` (`fecha`),
  KEY `idx_interes_contactado` (`contactado`),
  KEY `idx_interes_email` (`email`),
  CONSTRAINT `fk_interes_inmueble` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 9. TABLA IMAGEN_INMUEBLE - Imágenes de propiedades
-- ==============================================
CREATE TABLE `imagen_inmueble` (
  `id_imagen` int(11) NOT NULL AUTO_INCREMENT,
  `id_inmueble` int(11) NOT NULL,
  `url` varchar(255) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `orden` int(11) DEFAULT 0,
  `fecha_creacion` datetime DEFAULT current_timestamp(),
  PRIMARY KEY (`id_imagen`),
  KEY `idx_imagen_inmueble` (`id_inmueble`),
  KEY `idx_imagen_orden` (`orden`),
  CONSTRAINT `fk_imagen_inmueble` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- 10. TABLA CONTACTO - Formularios de contacto
-- ==============================================
CREATE TABLE `contacto` (
  `id_contacto` int(11) NOT NULL AUTO_INCREMENT,
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
  `user_agent` text DEFAULT NULL,
  PRIMARY KEY (`id_contacto`),
  KEY `idx_contacto_fecha` (`fecha_contacto`),
  KEY `idx_contacto_estado` (`estado`),
  KEY `idx_contacto_email` (`email`),
  KEY `idx_contacto_inmueble` (`id_inmueble`),
  CONSTRAINT `fk_contacto_inmueble` FOREIGN KEY (`id_inmueble`) REFERENCES `inmueble` (`id_inmueble`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ==============================================
-- REACTIVAR FOREIGN KEYS
-- ==============================================
SET FOREIGN_KEY_CHECKS = 1;

-- ==============================================
-- INSERTAR TIPOS DE INMUEBLE BÁSICOS
-- ==============================================
INSERT INTO `tipo_inmueble` (`id_tipo_inmueble`, `nombre`, `descripcion`, `fecha_creacion`, `estado`) VALUES
(1, 'Departamento', 'Unidad residencial en edificio', current_timestamp(), 1),
(2, 'Casa', 'Vivienda unifamiliar', current_timestamp(), 1),
(3, 'Local', 'Espacio comercial', current_timestamp(), 1),
(4, 'Depósito', 'Almacén o bodega', current_timestamp(), 1),
(5, 'Oficina', 'Espacio de trabajo comercial', current_timestamp(), 1),
(6, 'Local Comercial', 'Espacio destinado a actividad comercial', current_timestamp(), 1),
(7, 'Galpón', 'Espacio amplio para depósito o industria', current_timestamp(), 1),
(8, 'Terreno', 'Lote baldío para construcción', current_timestamp(), 1),
(9, 'PH (Propiedad Horizontal)', 'Casa en propiedad horizontal', current_timestamp(), 1),
(10, 'Cochera', 'Espacio para estacionamiento de vehículos', current_timestamp(), 1),
(11, 'Quinta', 'Propiedad rural con casa y terreno amplio', current_timestamp(), 1),
(12, 'Consultorio', 'Espacio para atención médica o profesional', current_timestamp(), 1),
(13, 'Loft', 'Espacio amplio sin divisiones internas', current_timestamp(), 1),
(14, 'Studio', 'Ambiente único que combina dormitorio y living', current_timestamp(), 1),
(15, 'Cabaña', 'Vivienda rústica, generalmente en zona rural', current_timestamp(), 1);

-- ==============================================
-- CONFIGURAR AUTO_INCREMENT
-- ==============================================
ALTER TABLE `usuario` AUTO_INCREMENT = 1;
ALTER TABLE `tipo_inmueble` AUTO_INCREMENT = 16;
ALTER TABLE `propietario` AUTO_INCREMENT = 1;
ALTER TABLE `inquilino` AUTO_INCREMENT = 1;
ALTER TABLE `inmueble` AUTO_INCREMENT = 1;
ALTER TABLE `contrato` AUTO_INCREMENT = 1;
ALTER TABLE `pago` AUTO_INCREMENT = 1;
ALTER TABLE `interes_inmueble` AUTO_INCREMENT = 1;
ALTER TABLE `imagen_inmueble` AUTO_INCREMENT = 1;
ALTER TABLE `contacto` AUTO_INCREMENT = 1;

-- ==============================================
-- VERIFICAR ESTRUCTURA CREADA
-- ==============================================
SELECT 'TABLAS CREADAS:' as verificacion;
SHOW TABLES;

SELECT 'TOTAL DE TABLAS:' as verificacion;
SELECT COUNT(*) as total_tablas FROM information_schema.tables 
WHERE table_schema = 'inmobiliaria';

-- ==============================================
-- VERIFICAR FOREIGN KEYS
-- ==============================================
SELECT 'FOREIGN KEYS CONFIGURADAS:' as verificacion;
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    CONSTRAINT_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE 
WHERE TABLE_SCHEMA = 'inmobiliaria' 
AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME, COLUMN_NAME;

-- ==============================================
-- SCRIPT COMPLETADO
-- ==============================================
-- Se crearon 10 tablas:
-- 1. usuario (tabla principal)
-- 2. tipo_inmueble (con 15 tipos precargados)
-- 3. propietario (relacionada con usuario)
-- 4. inquilino (relacionada con usuario)
-- 5. inmueble (relacionada con propietario y tipo_inmueble)
-- 6. contrato (relacionada con inmueble, inquilino, propietario, usuarios)
-- 7. pago (relacionada con contrato y usuarios)
-- 8. interes_inmueble (relacionada con inmueble)
-- 9. imagen_inmueble (relacionada con inmueble)
-- 10. contacto (opcional: relacionada con inmueble)
-- ==============================================