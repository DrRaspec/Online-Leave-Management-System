# Role and Department Guide

This document explains the difference between each user role in the Online Leave Management System and how departments affect access.

## Roles

### Admin

- Full system access.
- Can manage users.
- Can manage company settings.
- Can review leave requests.
- Can view leave reports for all departments.
- Can select any department in management and reporting screens.

### HR

- Can review leave requests.
- Can view leave reports for all departments.
- Can select any department in management and reporting screens.
- Cannot manage users.
- Cannot manage company settings.

### DepartmentAdmin

- Can review leave requests.
- Can view leave reports.
- Access is limited to their own department.
- Cannot manage users.
- Cannot manage company settings.
- Cannot freely switch to other departments.

### User

- Can submit leave requests.
- Can view their own leave information.
- Cannot review other users' leave requests.
- Cannot access management or company settings pages.

## Department Behavior

Departments do not have different built-in permissions by themselves.

The department mainly controls scope, especially for `DepartmentAdmin`.

- `Admin`: department does not limit access.
- `HR`: department does not limit access.
- `DepartmentAdmin`: can only manage and report on users in their own department.
- `User`: department is mostly used as profile and organizational information.

## Simple Summary Table

| Role | Manage Users | Company Settings | Review Requests | View Reports | Access Scope |
| --- | --- | --- | --- | --- | --- |
| Admin | Yes | Yes | Yes | Yes | All departments |
| HR | No | No | Yes | Yes | All departments |
| DepartmentAdmin | No | No | Yes | Yes | Own department only |
| User | No | No | No | No | Own records only |

## Notes

- A department like `IT`, `HR`, `Finance`, or `Operations` does not automatically get special behavior.
- The important difference is the assigned role, not the department name.
- The only role that depends strongly on department is `DepartmentAdmin`.
