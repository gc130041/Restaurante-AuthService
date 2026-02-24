import { body } from "express-validator";
import { validarCampos } from "./check-validators.js";
import { validarJWT } from "./validate-jwt.js";

export const createOrderValidator = [
    validarJWT,
    body("table", "El ID de la mesa es obligatorio y debe ser válido").isMongoId(),
    body("restaurant", "El ID del restaurante es obligatorio").isMongoId(),
    body("items", "Los items son obligatorios y deben ser un arreglo").isArray({ min: 1 }),
    body("items.*.menuItem", "El ID del platillo debe ser válido").isMongoId(),
    body("items.*.quantity", "La cantidad debe ser un número mayor a 0").isInt({ min: 1 }),
    body("items.*.price", "El precio del platillo es obligatorio").isNumeric(),
    body("items.*.modifiers", "Los modificadores deben ser un arreglo").optional().isArray(),
    validarCampos
];

export const updateItemStatusValidator = [
    validarJWT,
    body("status", "El estado es obligatorio").notEmpty(),
    body("status", "Estado no válido").isIn(["EN_ESPERA", "EN_COCINA", "LISTO", "SERVIDO"]),
    validarCampos
];

export const updateOrderStatusValidator = [
    validarJWT,
    body("status", "El estado es obligatorio").notEmpty(),
    body("status", "Estado no válido").isIn(["ABIERTA", "CERRADA", "CANCELADA"]),
    validarCampos
];