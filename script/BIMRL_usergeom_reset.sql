DROP TABLE USERGEOM_GEOMETRY_BCK;
CREATE TABLE USERGEOM_GEOMETRY_BCK AS SELECT * FROM USERGEOM_GEOMETRY;
TRUNCATE TABLE USERGEOM_GEOMETRY;
DROP TABLE USERGEOM_TOPO_FACE_BCK;
CREATE TABLE USERGEOM_TOPO_FACE_BCK AS SELECT * FROM USERGEOM_TOPO_FACE;
TRUNCATE TABLE USERGEOM_TOPO_FACE;
DROP TABLE USERGEOM_SPATIALINDEX_BCK;
CREATE TABLE USERGEOM_SPATIALINDEX_BCK AS SELECT * FROM USERGEOM_SPATIALINDEX;
TRUNCATE TABLE USERGEOM_SPATIALINDEX;
