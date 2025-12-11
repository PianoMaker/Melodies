function scaleContext(scaleFactor, context) {
    try {
        if (scaleFactor !== 1 && typeof context.scale === 'function') {
            context.scale(scaleFactor, scaleFactor);
        }
    } catch (e) {
        console.warn("Scaling failed:", e);
    }
}