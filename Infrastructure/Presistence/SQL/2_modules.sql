-- =============================================
-- MODULES TABLE (Base table for all modules)
-- =============================================
CREATE TABLE modules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    application_id UUID NOT NULL,
    module_type INTEGER NOT NULL,
    version VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    locked_by VARCHAR(255),
    created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_module_application FOREIGN KEY (application_id) 
        REFERENCES applications(id) ON DELETE CASCADE,
);

-- =============================================
-- MODULE TYPES
-- =============================================
-- 0 = Application
-- 1 = ProcessModule  
-- 2 = CalculateAction
-- 3 = CompareAction
-- 4 = DatabaseAction
-- 5 = FieldModule


-- =============================================
-- PROCESS MODULES TABLE
-- =============================================
CREATE TABLE process_modules (
    module_id UUID PRIMARY KEY,
    subtype VARCHAR(100),
    remote BOOLEAN DEFAULT FALSE,
    dynamic_call BOOLEAN DEFAULT FALSE,
    comment TEXT,
    CONSTRAINT fk_process_module FOREIGN KEY (module_id) 
        REFERENCES modules(id) ON DELETE CASCADE
);


-- =============================================
-- PROCESS MODULE DETAILS (Steps)
-- =============================================
CREATE TABLE process_module_details (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    process_module_id UUID NOT NULL,
    sequence INTEGER NOT NULL,
    label_name VARCHAR(255),
    action_type INTEGER,
    action_id UUID,
    action_module_type INTEGER,
    pass_label VARCHAR(255),
    fail_label VARCHAR(255),
    commented_flag BOOLEAN DEFAULT FALSE,
    comment TEXT,
    created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_process_module_detail FOREIGN KEY (process_module_id) 
        REFERENCES process_modules(module_id) ON DELETE CASCADE,
    CONSTRAINT uq_process_sequence UNIQUE (process_module_id, sequence),
    CONSTRAINT uq_process_label UNIQUE (process_module_id, label_name)
);

-- =============================================
-- ACTION TYPES
-- =============================================
-- 1 = Call
-- 2 = Return
-- 3 = DatabaseExecute


-- =============================================
-- DATABASE ACTION MODULES TABLE
-- =============================================
CREATE TABLE database_action_modules (
    module_id UUID PRIMARY KEY,
    statement TEXT NOT NULL,
    CONSTRAINT fk_database_action_module FOREIGN KEY (module_id) 
        REFERENCES modules(id) ON DELETE CASCADE
);


-- =============================================
-- FIELD MODULES TABLE
-- =============================================
CREATE TABLE field_modules (
    module_id UUID PRIMARY KEY,
    field_type INTEGER NOT NULL, -- 0=String, 1=Integer, 2=Boolean, 3=DateTime
    default_value TEXT,
    CONSTRAINT fk_field_module FOREIGN KEY (module_id) 
        REFERENCES modules(id) ON DELETE CASCADE
);

-- =============================================
-- FIELD TYPES
-- =============================================
-- 0 = String
-- 1 = Integer
-- 2 = Boolean
-- 3 = DateTime