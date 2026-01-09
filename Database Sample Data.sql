
-- =============================================
-- SAMPLE DATA FOR REPOSITORY DB
-- =============================================
-- This creates a complete working example:
-- Application: "Warehouse Management System"
-- Process: "Item Putaway Process" with 4 steps
-- Fields: Warehouse ID, Item ID, User ID, Location, Status, Message
-- Database Actions: Connect to WMS, Execute stored proc

-- =============================================
-- 1. CREATE APPLICATION
-- =============================================

INSERT INTO applications (id, name, version, version_build, last_compiled, last_activated, created_date, modified_date)
VALUES 
    ('a1111111-1111-1111-1111-111111111111', 'Warehouse Management System', '1.0', '1.0.0.1', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- =============================================
-- 2. CREATE FIELD MODULES (Type 5)
-- =============================================

-- Base Modules
INSERT INTO modules (id, application_id, module_type, version, name, description, created_date, modified_date)
VALUES
    -- Input Fields
    ('f0000001-0000-0000-0000-000000000001', 'a1111111-1111-1111-1111-111111111111', 5, 1, 'Warehouse ID', 'Warehouse identifier', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('f0000002-0000-0000-0000-000000000002', 'a1111111-1111-1111-1111-111111111111', 5, 1, 'Item ID', 'Item identifier', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('f0000003-0000-0000-0000-000000000003', 'a1111111-1111-1111-1111-111111111111', 5, 1, 'User ID', 'User performing the action', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('f0000004-0000-0000-0000-000000000004', 'a1111111-1111-1111-1111-111111111111', 5, 1, 'Location', 'Storage location', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    
    -- Output Fields
    ('f0000005-0000-0000-0000-000000000005', 'a1111111-1111-1111-1111-111111111111', 5, 1, 'Status Code', 'Return status code', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('f0000006-0000-0000-0000-000000000006', 'a1111111-1111-1111-1111-111111111111', 5, 1, 'Status Message', 'Return status message', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Field Module Details
INSERT INTO field_modules (module_id, field_type, default_value)
VALUES
    ('f0000001-0000-0000-0000-000000000001', 0, NULL),  -- Warehouse ID (String)
    ('f0000002-0000-0000-0000-000000000002', 0, NULL),  -- Item ID (String)
    ('f0000003-0000-0000-0000-000000000003', 1, NULL),  -- User ID (Integer)
    ('f0000004-0000-0000-0000-000000000004', 0, NULL),  -- Location (String)
    ('f0000005-0000-0000-0000-000000000005', 1, '0'),   -- Status Code (Integer, default 0)
    ('f0000006-0000-0000-0000-000000000006', 0, '');    -- Status Message (String, default empty)


-- =============================================
-- 3. CREATE DATABASE ACTION MODULES (Type 4)
-- =============================================

-- Base Modules for Database Actions
INSERT INTO modules (id, application_id, module_type, version, name, description, created_date, modified_date)
VALUES
    ('d0000001-0000-0000-0000-000000000001', 'a1111111-1111-1111-1111-111111111111', 4, 1, 'Connect to WMS', 'Connects to WMS database', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('d0000002-0000-0000-0000-000000000002', 'a1111111-1111-1111-1111-111111111111', 4, 1, 'Execute Putaway', 'Executes putaway stored procedure', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('d0000003-0000-0000-0000-000000000003', 'a1111111-1111-1111-1111-111111111111', 4, 1, 'Log Activity', 'Logs activity to engine database', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT INTO database_action_modules (module_id, statement)
VALUES
    ('d0000001-0000-0000-0000-000000000001', 'CONNECT WMS;'),
    
    ('d0000002-0000-0000-0000-000000000002',
     'SELECT * FROM warehouse_putaway(
         ::#5#f0000001-0000-0000-0000-000000000001#::,
         ::#5#f0000002-0000-0000-0000-000000000002#::,
         ::#5#f0000004-0000-0000-0000-000000000004#::,
         ::#5#f0000003-0000-0000-0000-000000000003#::
      )
      RETURNS(::#5#f0000005-0000-0000-0000-000000000005#::, ::#5#f0000006-0000-0000-0000-000000000006#::);'),
    
    ('d0000003-0000-0000-0000-000000000003',
     'CONNECT ENGINE;
      INSERT INTO activity_log (user_id, action, warehouse_id, item_id, status, created_at)
      VALUES (
         ::#5#f0000003-0000-0000-0000-000000000003#::,
         ''PUTAWAY'',
         ::#5#f0000001-0000-0000-0000-000000000001#::,
         ::#5#f0000002-0000-0000-0000-000000000002#::,
         ::#5#f0000005-0000-0000-0000-000000000005#::,
         CURRENT_TIMESTAMP
      );');

	  
-- =============================================
-- 4. CREATE PROCESS MODULE (Type 1)
-- =============================================

-- Base Module for Process
INSERT INTO modules (id, application_id, module_type, version, name, description, created_date, modified_date)
VALUES
    ('c0000001-0000-0000-0000-000000000001', 'a1111111-1111-1111-1111-111111111111', 1, 1, 'Item Putaway Process', 'Complete item putaway workflow', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Process Module Details
INSERT INTO process_modules (module_id, subtype, remote, dynamic_call, comment)
VALUES
    ('c0000001-0000-0000-0000-000000000001', 'Standard', FALSE, FALSE, 'Main putaway process for warehouse items');

-- Process Module Steps
INSERT INTO process_module_details (id, process_module_id, sequence, label_name, action_type, action_id, action_module_type, pass_label, fail_label, commented_flag, comment, created_date)
VALUES
    -- Step 1: Connect to WMS Database
    ('e0000001-0000-0000-0000-000000000001', 'c0000001-0000-0000-0000-000000000001', 1, 'START', 4, 'd0000001-0000-0000-0000-000000000001', 4, 'NEXT', 'ERROR_HANDLER', FALSE, 'Connect to WMS database', CURRENT_TIMESTAMP),

    -- Step 2: Execute Putaway Stored Procedure
    ('e0000002-0000-0000-0000-000000000002', 'c0000001-0000-0000-0000-000000000001', 2, 'EXECUTE_PUTAWAY', 4, 'd0000002-0000-0000-0000-000000000002', 4, 'NEXT', 'ERROR_HANDLER', FALSE, 'Execute putaway logic', CURRENT_TIMESTAMP),

    -- Step 3: Log Activity
    ('e0000003-0000-0000-0000-000000000003', 'c0000001-0000-0000-0000-000000000001', 3, 'LOG_ACTIVITY', 4, 'd0000003-0000-0000-0000-000000000003', 4, 'NEXT', 'ERROR_HANDLER', FALSE, 'Log the putaway activity', CURRENT_TIMESTAMP),
    
    -- Step 4: Return Success
    ('e0000004-0000-0000-0000-000000000004', 'c0000001-0000-0000-0000-000000000001', 4, 'SUCCESS', 2, NULL, NULL, 'PASS', 'PASS', FALSE, 'Process completed successfully', CURRENT_TIMESTAMP),
    
    -- Step 5: Error Handler (jumped to on failure)
    ('e0000005-0000-0000-0000-000000000005', 'c0000001-0000-0000-0000-000000000001', 5, 'ERROR_HANDLER', 3, NULL, NULL, 'FAIL', 'FAIL', FALSE, 'Handle errors', CURRENT_TIMESTAMP);



-- =============================================
-- VERIFICATION QUERIES
-- =============================================

-- Check what was created
SELECT 'Applications' AS entity, COUNT(*) AS count FROM applications WHERE id = 'a1111111-1111-1111-1111-111111111111'
UNION ALL
SELECT 'Modules', COUNT(*) FROM modules WHERE application_id = 'a1111111-1111-1111-1111-111111111111'
UNION ALL
SELECT 'Field Modules', COUNT(*) FROM field_modules WHERE module_id IN (SELECT id FROM modules WHERE application_id = 'a1111111-1111-1111-1111-111111111111')
UNION ALL
SELECT 'Database Actions', COUNT(*) FROM database_action_modules WHERE module_id IN (SELECT id FROM modules WHERE application_id = 'a1111111-1111-1111-1111-111111111111')
UNION ALL
SELECT 'Process Modules', COUNT(*) FROM process_modules WHERE module_id IN (SELECT id FROM modules WHERE application_id = 'a1111111-1111-1111-1111-111111111111')
UNION ALL
SELECT 'Process Steps', COUNT(*) FROM process_module_details WHERE process_module_id IN (SELECT module_id FROM process_modules WHERE module_id IN (SELECT id FROM modules WHERE application_id = 'a1111111-1111-1111-1111-111111111111'));






-- =============================================
-- SAMPLE DATA FOR WMS
-- =============================================
-- =============================================
-- 1. CREATE TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS warehouses (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    location VARCHAR(255),
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS items (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    sku VARCHAR(100),
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS locations (
    id VARCHAR(50) PRIMARY KEY,
    warehouse_id VARCHAR(50) REFERENCES warehouses(id),
    aisle VARCHAR(10),
    rack VARCHAR(10),
    shelf VARCHAR(10),
    bin VARCHAR(10),
    capacity INTEGER DEFAULT 100,
    current_quantity INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS inventory (
    id SERIAL PRIMARY KEY,
    warehouse_id VARCHAR(50) REFERENCES warehouses(id),
    item_id VARCHAR(50) REFERENCES items(id),
    location_id VARCHAR(50) REFERENCES locations(id),
    quantity INTEGER DEFAULT 0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by INTEGER
);

-- =============================================
-- 2. INSERT SAMPLE DATA
-- =============================================

-- Warehouses
INSERT INTO warehouses (id, name, location, active) VALUES
    ('WH001', 'Main Warehouse', 'New York', TRUE),
    ('WH002', 'West Coast Warehouse', 'Los Angeles', TRUE),
    ('WH003', 'Central Warehouse', 'Chicago', TRUE)
ON CONFLICT (id) DO NOTHING;

-- Items
INSERT INTO items (id, name, description, sku, active) VALUES
    ('ITEM001', 'Widget A', 'Standard widget type A', 'SKU-WGT-A-001', TRUE),
    ('ITEM002', 'Widget B', 'Premium widget type B', 'SKU-WGT-B-002', TRUE),
    ('ITEM003', 'Gadget X', 'Advanced gadget model X', 'SKU-GDG-X-003', TRUE),
    ('ITEM004', 'Component Z', 'Electronic component Z', 'SKU-CMP-Z-004', TRUE)
ON CONFLICT (id) DO NOTHING;

-- Locations
INSERT INTO locations (id, warehouse_id, aisle, rack, shelf, bin, capacity) VALUES
    ('LOC-A1-R1-S1-B1', 'WH001', 'A1', 'R1', 'S1', 'B1', 100),
    ('LOC-A1-R1-S1-B2', 'WH001', 'A1', 'R1', 'S1', 'B2', 100),
    ('LOC-A1-R1-S2-B1', 'WH001', 'A1', 'R1', 'S2', 'B1', 100),
    ('LOC-A2-R1-S1-B1', 'WH001', 'A2', 'R1', 'S1', 'B1', 100),
    ('LOC-B1-R1-S1-B1', 'WH002', 'B1', 'R1', 'S1', 'B1', 100),
    ('LOC-B1-R1-S1-B2', 'WH002', 'B1', 'R1', 'S1', 'B2', 100),
    ('LOC-C1-R1-S1-B1', 'WH003', 'C1', 'R1', 'S1', 'B1', 100)
ON CONFLICT (id) DO NOTHING;

-- =============================================
-- 3. CREATE STORED PROCEDURE
-- =============================================

CREATE OR REPLACE FUNCTION warehouse_putaway(
    p_warehouse_id VARCHAR(50),
    p_item_id VARCHAR(50),
    p_location_id VARCHAR(50),
    p_user_id INTEGER
)
RETURNS TABLE(status_code INTEGER, status_message VARCHAR(255))
LANGUAGE plpgsql
AS $$
DECLARE
    v_warehouse_exists BOOLEAN;
    v_item_exists BOOLEAN;
    v_location_exists BOOLEAN;
    v_location_warehouse VARCHAR(50);
    v_current_quantity INTEGER;
    v_capacity INTEGER;
BEGIN
    -- Validate warehouse exists and is active
    SELECT EXISTS(SELECT 1 FROM warehouses WHERE id = p_warehouse_id AND active = TRUE)
    INTO v_warehouse_exists;
    
    IF NOT v_warehouse_exists THEN
        RETURN QUERY SELECT 1::INTEGER, 'Error: Warehouse not found or inactive'::VARCHAR(255);
        RETURN;
    END IF;
    
    -- Validate item exists and is active
    SELECT EXISTS(SELECT 1 FROM items WHERE id = p_item_id AND active = TRUE)
    INTO v_item_exists;
    
    IF NOT v_item_exists THEN
        RETURN QUERY SELECT 2::INTEGER, 'Error: Item not found or inactive'::VARCHAR(255);
        RETURN;
    END IF;
    
    -- Validate location exists and belongs to the warehouse
    SELECT warehouse_id, current_quantity, capacity
    INTO v_location_warehouse, v_current_quantity, v_capacity
    FROM locations
    WHERE id = p_location_id;
    
    IF v_location_warehouse IS NULL THEN
        RETURN QUERY SELECT 3::INTEGER, 'Error: Location not found'::VARCHAR(255);
        RETURN;
    END IF;
    
    IF v_location_warehouse != p_warehouse_id THEN
        RETURN QUERY SELECT 4::INTEGER, 'Error: Location does not belong to specified warehouse'::VARCHAR(255);
        RETURN;
    END IF;
    
    -- Check capacity
    IF v_current_quantity >= v_capacity THEN
        RETURN QUERY SELECT 5::INTEGER, 'Error: Location is at full capacity'::VARCHAR(255);
        RETURN;
    END IF;
    
    -- Insert or update inventory
    INSERT INTO inventory (warehouse_id, item_id, location_id, quantity, updated_by, last_updated)
    VALUES (p_warehouse_id, p_item_id, p_location_id, 1, p_user_id, CURRENT_TIMESTAMP)
    ON CONFLICT ON CONSTRAINT inventory_warehouse_id_item_id_location_id_key
    DO UPDATE SET 
        quantity = inventory.quantity + 1,
        updated_by = p_user_id,
        last_updated = CURRENT_TIMESTAMP;
    
    -- Update location current quantity
    UPDATE locations
    SET current_quantity = current_quantity + 1
    WHERE id = p_location_id;
    
    -- Return success
    RETURN QUERY SELECT 0::INTEGER, 'Success: Item putaway completed'::VARCHAR(255);
    
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT 99::INTEGER, ('Error: ' || SQLERRM)::VARCHAR(255);
END;
$$;

-- Add unique constraint for inventory (if not exists)
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'inventory_warehouse_id_item_id_location_id_key'
    ) THEN
        ALTER TABLE inventory 
        ADD CONSTRAINT inventory_warehouse_id_item_id_location_id_key 
        UNIQUE (warehouse_id, item_id, location_id);
    END IF;
END $$;

-- =============================================
-- 4. TEST THE STORED PROCEDURE
-- =============================================

-- Test successful putaway
SELECT * FROM warehouse_putaway('WH001', 'ITEM001', 'LOC-A1-R1-S1-B1', 123);

-- Test invalid warehouse
SELECT * FROM warehouse_putaway('INVALID', 'ITEM001', 'LOC-A1-R1-S1-B1', 123);

-- Test invalid item
SELECT * FROM warehouse_putaway('WH001', 'INVALID', 'LOC-A1-R1-S1-B1', 123);

-- Test invalid location
SELECT * FROM warehouse_putaway('WH001', 'ITEM001', 'INVALID', 123);





-- =============================================
-- ENGINE DB
-- =============================================


CREATE TABLE IF NOT EXISTS activity_log (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    action VARCHAR(100) NOT NULL,
    warehouse_id VARCHAR(50),
    item_id VARCHAR(50),
    status INTEGER,
    message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert sample activity
INSERT INTO activity_log (user_id, action, warehouse_id, item_id, status, message)
VALUES 
    (100, 'PUTAWAY', 'WH001', 'ITEM001', 0, 'Test putaway completed successfully');


INSERT INTO activity_log (user_id, action, warehouse_id, item_id, status, created_at)
      VALUES (
         123,
         'PUTAWAY',
         'WH001',
         'ITEM001',
         System.Object[],
         CURRENT_TIMESTAMP
      );