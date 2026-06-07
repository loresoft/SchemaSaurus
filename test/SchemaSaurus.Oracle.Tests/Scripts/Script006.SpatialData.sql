DECLARE
    table_count NUMBER;
    mdsys_spatial_type_count NUMBER;
    spatial_type_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO mdsys_spatial_type_count
    FROM ALL_TYPES
    WHERE OWNER = 'MDSYS'
      AND TYPE_NAME = 'SDO_GEOMETRY';

    IF mdsys_spatial_type_count = 0 THEN
        SELECT COUNT(*) INTO spatial_type_count
        FROM USER_TYPES
        WHERE TYPE_NAME = 'SDO_GEOMETRY';

        IF spatial_type_count = 0 THEN
            EXECUTE IMMEDIATE '
                CREATE TYPE SDO_GEOMETRY AS OBJECT
                (
                    WKT CLOB
                )';
        END IF;
    END IF;

    SELECT COUNT(*) INTO table_count
    FROM USER_TABLES
    WHERE TABLE_NAME = 'SPATIALDATA';

    IF table_count = 0 THEN
        IF mdsys_spatial_type_count = 0 THEN
            EXECUTE IMMEDIATE '
                CREATE TABLE SpatialData
                (
                    Id NUMBER(10) NOT NULL PRIMARY KEY,
                    GeometryValue SDO_GEOMETRY NULL
                )';
        ELSE
            EXECUTE IMMEDIATE '
                CREATE TABLE SpatialData
                (
                    Id NUMBER(10) NOT NULL PRIMARY KEY,
                    GeometryValue MDSYS.SDO_GEOMETRY NULL
                )';
        END IF;
    END IF;
END;
