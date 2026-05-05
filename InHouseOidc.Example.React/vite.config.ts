import { fileURLToPath, URL } from 'node:url';
import { defineConfig, createLogger } from 'vite';
import react from '@vitejs/plugin-react'

const logger = createLogger();
const loggerError = logger.error;
logger.error = (msg, options) => {
    if (msg.includes('ECONNREFUSED')) return;
    loggerError(msg, options);
};

// https://vitejs.dev/config/
export default defineConfig({
    customLogger: logger,
    plugins: [react()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api': {
                target: "http://localhost:5104",
                secure: false
            }
        },
        port: 5105,
    }
})
