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

- **(pendiente)** Verificar que todos los endpoints relacionados con **notifications, posts, tags, follow, user status**
estén correctamente documentados en Swagger

9️⃣ **Testing / QA**

- **(pendiente)** Test unitarios e integración para:

  - NotificationService + NotificationController

  - PostService

  - TagService

  - FollowService

  - UserStatusService

🔹 **Pendiente / opcional**

- **(pendiente)** Integración con Camila:

  - Asegurarse que los tags en templates funcionan para notificaciones

  - Flujo de posts y feed compatible con front / CLI