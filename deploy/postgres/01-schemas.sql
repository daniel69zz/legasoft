-- PostgreSQL central: una instancia, un schema por dominio y un usuario por servicio (ADR-005).
-- Contraseñas solo para desarrollo local; en producción se inyectan como secretos.

CREATE ROLE identity_svc LOGIN PASSWORD 'identity_dev';
CREATE ROLE sync_svc     LOGIN PASSWORD 'sync_dev';
CREATE ROLE business_svc LOGIN PASSWORD 'business_dev';
CREATE ROLE reporting_ro LOGIN PASSWORD 'reporting_dev';

CREATE SCHEMA identity  AUTHORIZATION identity_svc;
CREATE SCHEMA sync      AUTHORIZATION sync_svc;
CREATE SCHEMA sales     AUTHORIZATION business_svc;
CREATE SCHEMA inventory AUTHORIZATION business_svc;
CREATE SCHEMA cash      AUTHORIZATION business_svc;
CREATE SCHEMA audit     AUTHORIZATION business_svc;

-- Nadie crea objetos en public.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

-- Lectura explícita para reportes consolidados que cruzan dominios.
GRANT USAGE ON SCHEMA sales, inventory, cash, audit TO reporting_ro;
ALTER DEFAULT PRIVILEGES FOR ROLE business_svc IN SCHEMA sales, inventory, cash, audit
  GRANT SELECT ON TABLES TO reporting_ro;
