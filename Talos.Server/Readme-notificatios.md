## ✅ CheckList “Sarah – Backend Core”
1️⃣ **Base de Datos**

- ✅Tablas creadas: **Notification, Tag, Follow, Post**

- ✅Campos agregados a **User**: templates, posts, followers/following

2️⃣ **Notification System**

- ✅**NotificationService** implementado:

  - Crear notificación → `CreateAsync`

  - Listar notificaciones → `GetUserNotificationsAsync`

  - Marcar individual como leída → `MarkAsReadAsync`

  - Marcar todas como leídas → `MarkAllAsReadAsync`

- ✅**NotificationController** implementado con endpoints:

  - `GET /api/notifications`

  - `PUT /api/notifications/{id}/read`

  - `PUT /api/notifications/read-all`

3️⃣ **User Status**

- ✅**UserStatusService** implementado:

  - Marcar usuario online/offline

  - Obtener estado de todos los usuarios → `GetUsersStatusAsync`

- ✅Endpoint `GET /api/users/status` implementado en **UsersController**

4️⃣ **Follow System**

- ✅**FollowService** implementado:

  - Seguir / dejar de seguir → `FollowUserAsync`, `UnfollowUserAsync`

  - Validar si un usuario sigue a otro → `IsFollowingAsync`

  - Listar seguidores / siguiendo → `GetFollowersAsync`, `GetFollowingAsync`

- ✅Endpoints en UsersController:

  - `GET /api/users/{id}/followers`

  - `GET /api/users/{id}/following`

5️⃣ **Post System**

- ✅**PostService** implementado:

  - Crear post → `CreatePostAsync`

  - Listar posts de usuario → `GetUserPostsAsync`

  - Obtener feed de posts → `GetFeedAsync`

- ✅Soporte de **tags** en posts

6️⃣ **Tag System**

- ✅**TagService** implementado:

  - Crear / eliminar tags

  - Listar todos los tags

  - Obtener tag por Id

- 7️⃣ **Cache & Performance**

- ✅Redis / DistributedCache implementado para:

  - Usuarios online

  - Templates de usuario

  - Estadísticas y búsquedas

8️⃣ **Swagger / Documentación**

- ✅ Verifiqué que todos los endpoints relacionados con:

  - **Notifications**

  - **Posts**

  - **Tags**

  - **Follow**

  - **User Status**

estén correctamente expuestos y visibles en **Swagger UI**.

- ✅ Validé:

  - Rutas

  - Métodos HTTP correctos (GET, POST, PUT, DELETE)

  - Requisitos de autenticación (`[Authorize]`)

  - Estructura de request / response esperada

- ✅ Ajusté respuestas para evitar errores de serialización (ciclos entre entidades),
utilizando **DTOs específicos** en endpoints críticos como posts y feed.

9️⃣ **Testing / QA**

- ✅ Testing manual exhaustivo usando **Swagger + Postman** para:

  - **NotificationController**

    - Crear notificaciones

    - Listar notificaciones

    - Marcar como leídas

    - Marcar todas como leídas

  - **PostService**

    - Crear post

    - Obtener feed

    - Verificación de tags asociados

  - **TagService**

    - Creación y asociación de tags

    - Uso compartido entre templates, posts y notificaciones

  - **UserStatusService**

    - Actualización de `LastSeenAt`

    - Verificación de usuarios online/offline

  - ✅ Validación de:

    - Autenticación JWT

    - Permisos por usuario
    - Persistencia correcta en base de datos (MySQL en Aiven)

- 9️⃣ **Implementacion de Semantic Kernel AI**