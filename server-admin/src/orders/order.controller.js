import Order from "./order.model.js";
import Table from "../tables/table.model.js";

export const createOrder = async (req, res) => {
    try {
        const { table, restaurant, items } = req.body;
        const waiter = req.usuario._id; 

        const existingOrder = await Order.findOne({ table, status: "ABIERTA" });
        if (existingOrder) {
            return res.status(400).json({
                success: false,
                message: "Operación denegada: La mesa ya tiene una comanda abierta actualmente."
            });
        }

        const tableExists = await Table.findById(table);
        if (!tableExists) {
            return res.status(404).json({ 
                success: false, 
                message: "La mesa especificada no existe." 
            });
        }

        const newOrder = new Order({
            table,
            waiter,
            restaurant,
            items
        });

        await newOrder.save();

        res.status(201).json({
            success: true,
            message: "Comanda creada y vinculada exitosamente",
            order: newOrder
        });

    } catch (error) {
        console.error("Error al crear la orden:", error);
        res.status(500).json({
            success: false,
            message: "Error interno del servidor al crear la comanda",
            error: error.message
        });
    }
};

export const updateItemStatus = async (req, res) => {
    try {
        const { orderId, itemId } = req.params;
        const { status } = req.body;

        const order = await Order.findOneAndUpdate(
            { _id: orderId, "items._id": itemId },
            { $set: { "items.$.status": status } },
            { new: true }
        );

        if (!order) {
            return res.status(404).json({
                success: false,
                message: "No se encontró la orden o el platillo especificado."
            });
        }

        res.status(200).json({
            success: true,
            message: `Estado del platillo actualizado a ${status}`,
            order
        });

    } catch (error) {
        console.error("Error al actualizar estado del platillo:", error);
        res.status(500).json({
            success: false,
            message: "Error interno del servidor al actualizar el platillo",
            error: error.message
        });
    }
};

export const updateOrderStatus = async (req, res) => {
    try {
        const { id } = req.params;
        const { status } = req.body;

        const order = await Order.findByIdAndUpdate(
            id,
            { status },
            { new: true }
        );

        if (!order) {
            return res.status(404).json({
                success: false,
                message: "Comanda no encontrada."
            });
        }

        res.status(200).json({
            success: true,
            message: `Estado de la comanda actualizado a ${status}`,
            order
        });

    } catch (error) {
        console.error("Error al actualizar estado de la orden:", error);
        res.status(500).json({
            success: false,
            message: "Error interno del servidor",
            error: error.message
        });
    }
};