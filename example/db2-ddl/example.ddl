-- - - - - - - - - - - - D D L / D C L   F O L L O W S - - - - - - - - - - -
-- WARNING: HDDL MAY GENERATE COMMENTED OUT SQL FOR IMPLICITLY CREATED
--          OBJECTS. THESE OBJECTS CAN BE LOCATED BY EXECUTING A FIND
--          FOR THE TEXT '-- COMMENTED IMPLICIT' IN THE OUTPUT DDL.
--
     CREATE DATABASE
       DB001
         BUFFERPOOL BP1
         INDEXBP BP2
         STOGROUP STO001
         CCSID EBCDIC
     ;
     CREATE
      TABLESPACE TABLESPACE1
         IN DB001
         USING STOGROUP STO001
         PRIQTY 10800
         SECQTY 10800
         ERASE NO
         FREEPAGE 30
         PCTFREE 20
         GBPCACHE CHANGED
         COMPRESS YES
         TRACKMOD NO
         LOGGED
         DSSIZE 4 G
         SEGSIZE 64
         MAXPARTITIONS 16
         BUFFERPOOL BP1
         LOCKSIZE ANY
         LOCKMAX SYSTEM
         CLOSE YES
         CCSID EBCDIC
         MAXROWS 255
     ;
     CREATE TABLE
       SCOPE.TABLE1
        (
        FIELD1  CHAR(11) NOT NULL
         FOR SBCS DATA
       ,FIELD2  CHAR(1) NOT NULL WITH DEFAULT
         FOR SBCS DATA
       ,FIEDL3  DATE
        ,
         CONSTRAINT PK_TABLE1
         PRIMARY KEY
         (
          FIELD1
         )
        )
         IN DB001.TABLESPACE1
         DATA CAPTURE CHANGES
         CCSID EBCDIC
         NOT VOLATILE
         APPEND NO
     ;
     CREATE UNIQUE
        INDEX SCOPE.INDEX1
       ON SCOPE.TABLE1
        (
         FIELD1 ASC
        )
         USING STOGROUP STO001
         PRIQTY 10800
         SECQTY 10800
         ERASE NO
         FREEPAGE 50
         PCTFREE 10
         GBPCACHE CHANGED
         CLUSTER
         BUFFERPOOL BP2
         CLOSE YES
         COPY NO
         PIECESIZE 2 G
         COMPRESS NO
     ;
     CREATE VIEW
       SCOPE.VIEW1
AS SELECT * FROM SCOPE.TABLE1
     ;
     ALTER TABLE
       SCOPE.TABLE1
       ADD
         CONSTRAINT FK1
         FOREIGN KEY
         (
          FIELD1
         )
         REFERENCES
         SCOPE.TABLE1
         (
          FIELD2
         )
         ON DELETE RESTRICT
         ENFORCED
     ;
