# Asignación y gestión de roles

La asignación y modificación de roles solo puede realizarla un usuario con rol **Administrador**.

---

## Pasos para asignar o cambiar un rol

1. Iniciar sesión con un usuario **Administrador**.
2. Ir al menú superior y seleccionar **“Gestión de Usuarios”**.
3. En la tabla de usuarios, presionar el botón **“Cambiar Rol”** sobre el usuario deseado.
4. En la pantalla de asignación se mostrará:
   - Nombre y correo del usuario.
   - Rol actual.
   - Un selector con los roles disponibles:
     - Administrador
     - Veterinario
     - Cliente
5. Seleccionar el nuevo rol y confirmar el cambio.

---

## Desasignación o cambio de rol

Los roles no se eliminan directamente, sino que se reemplazan por otro.  
El sistema ajusta automáticamente las entidades relacionadas según el rol asignado:

| Rol asignado | Acciones automáticas del sistema |
|--------------|---------------------------------|
| **Veterinario** | Se crea un registro en la tabla `Veterinarios`. <br> Si el usuario era Cliente, se elimina su registro en `Clientes`. |
| **Cliente**    | Se crea un registro en la tabla `Clientes`. <br> Si el usuario era Veterinario, se eliminan sus turnos activos y su registro en `Veterinarios`. |
| **Administrador** | Se eliminan los perfiles asociados de Cliente o Veterinario. |
