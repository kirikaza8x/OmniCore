SELECT 'CREATE DATABASE omnicore_auth_db' 
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'omnicore_auth_db')\gexec

SELECT 'CREATE DATABASE omnicore_user_db' 
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'omnicore_user_db')\gexec

SELECT 'CREATE DATABASE omnicore_notification_db' 
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'omnicore_notification_db')\gexec