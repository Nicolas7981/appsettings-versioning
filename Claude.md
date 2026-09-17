## Contexto (ya resuelto — no investigar ni releer la propuesta original)

- Objetivo: validar en Jenkins, con paridad 1:1, las Opciones 1 (Replace Tokens)
  y 2 (sustitución nativa de JSON, la clave vacía que ASP.NET Core llena con
  variables de entorno) de la propuesta "Gestión segura de configuraciones
  sensibles".
- Jenkins ya corre en Docker (contenedor `jenkins-beta`), con Git, Pipeline y
  Credentials Binding instalados.
- Esto es una beta desechable. Prioriza que funcione y sea rápido de validar,
  no dejarlo "production-ready".

## Decisión ya tomada: proyecto .NET nuevo, no buscar uno existente

No clones ni busques un repo de ejemplo en GitHub. Un proyecto de 1 archivo de
config + 1 endpoint es suficiente para demostrar ambas opciones, y evita
arrastrar dependencias o una versión de SDK que no controlas. No reabras esta
decisión ni gastes una búsqueda web en ello.

## Tareas (ejecutar en orden; no pedir confirmación entre pasos, salvo el paso 5)

1. `dotnet new webapi -n ConfigPocApi -minimal --no-https -o config-poc`
   (.NET 8).
2. En `config-poc/appsettings.json` dejar dos variantes en archivos separados:
   - `appsettings.opcion1.json` → placeholder de token:
     `"ConnectionStrings": { "DefaultConnection": "#{ConnectionStrings.DefaultConnection}#" }`
   - `appsettings.opcion2.json` → clave vacía (esqueleto):
     `"ConnectionStrings": { "DefaultConnection": "" }`
3. Agregar un único endpoint mínimo en `Program.cs`:
   `app.MapGet("/config", (IConfiguration c) => c["ConnectionStrings:DefaultConnection"]);`
   Esto es todo lo necesario para verificar en runtime qué valor quedó inyectado.
4. `git init`, `dotnet new gitignore`, commit inicial.
5. **Detente aquí si no tengo el repo remoto configurado.** Preguntarme el
   `REPO_URL` real antes de continuar — no inventar ni usar un placeholder
   para el push.
6. `git remote add origin <REPO_URL>`.
7. Agregar `Jenkinsfile` en la raíz con dos stages:
   - `Opcion1_ReplaceTokens`: usa `npx @qetza/replacetokens-cli` sobre
     `appsettings.opcion1.json`, tomando el valor desde una credencial de
     Jenkins tipo "Secret text" (`DB_ConnectionString_Dev`).
   - `Opcion2_NativeSubstitution`: exporta la variable de entorno
     `ConnectionStrings__DefaultConnection` (doble guion bajo) desde la misma
     credencial y corre `dotnet run --project config-poc` para confirmar que
     `/config` devuelve el valor inyectado sin tocar el archivo.
8. `git push -u origin main`.
9. Un solo `dotnet build` de humo basta — no repetir builds para "asegurarse".

## Reglas de eficiencia de tokens (para toda la sesión)

- No releer archivos completos que tú mismo escribiste en este turno.
- Editar con `str_replace`/parches puntuales; nunca reescribir el proyecto
  completo por cambiar un valor.
- No narrar cada comando de `dotnet`/`git` en prosa; reportar solo si falla.
- Agrupar comandos de shell independientes en una sola llamada (`&&`) en vez
  de una tool call por comando.
- No buscar en la web patrones de "buenas prácticas de appsettings.json" ni
  similares — el esquema ya está definido arriba.
- Al terminar: resumen de máximo 5 líneas — qué se creó, qué falta (repo URL
  si no se dio, credenciales de Jenkins) y el siguiente comando manual a
  correr por el usuario.