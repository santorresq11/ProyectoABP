export const environment = {
  production: true,
  // El flujo de despliegue (.github/workflows/deploy.yml) reemplaza este valor
  // por la URL del App Service que devuelve infra/main.bicep (salida apiUrl).
  apiUrl: 'http://localhost:5125',
};
