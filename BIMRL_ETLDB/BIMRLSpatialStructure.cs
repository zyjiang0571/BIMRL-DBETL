﻿//
// BIMRL (BIM Rule Language) Simplified Schema ETL (Extract, Transform, Load) library: this library transforms IFC data into BIMRL Simplified Schema for RDBMS. 
// This work is part of the original author's Ph.D. thesis work on the automated rule checking in Georgia Institute of Technology
// Copyright (C) 2013 Wawan Solihin (borobudurws@hotmail.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
#if ORACLE
using Oracle.DataAccess.Types;
using Oracle.DataAccess.Client;
#endif
#if POSTGRES
using Npgsql;
using NpgsqlTypes;
#endif
using BIMRL.Common;

namespace BIMRL
{
   class BIMRLSpatialStructure
   {
      IfcStore _model;
      BIMRLCommon _refBIMRLCommon;

      public BIMRLSpatialStructure(IfcStore m, BIMRLCommon refBIMRLCommon)
      {
         _model = m;
         _refBIMRLCommon = refBIMRLCommon;
      }

      public void processElementStructure()
      {
         processSpatialStructureData();
         processSpatialStructureRel();
      }

      private void processSpatialStructureData()
      {
         string currStep = string.Empty;
         DBOperation.beginTransaction();
         string container = string.Empty;
         int commandStatus = -1;
         int currInsertCount = 0;
         string sqlStmt;

#if ORACLE
         OracleCommand command = new OracleCommand(" ", DBOperation.DBConn);
#endif
#if POSTGRES
         NpgsqlCommand command = new NpgsqlCommand(" ", DBOperation.DBConn);
         sqlStmt = "Insert into " + DBOperation.formatTabName("BIMRL_Element") + "(Elementid, LineNo, ElementType, ModelID, Name, LongName, Description, ObjectType, Container, TypeID) "
            + "Values (@eid, @line, @etyp, @modid, @name, @lname, @desc, @otyp, @cont, @typid)";
         command.CommandText = sqlStmt;

         command.Parameters.Add("@eid", NpgsqlDbType.Text);
         command.Parameters.Add("@line", NpgsqlDbType.Integer);
         command.Parameters.Add("@etyp", NpgsqlDbType.Text);
         command.Parameters.Add("@modid", NpgsqlDbType.Integer);
         command.Parameters.Add("@name", NpgsqlDbType.Text);
         command.Parameters.Add("@lname", NpgsqlDbType.Text);
         command.Parameters.Add("@desc", NpgsqlDbType.Text);
         command.Parameters.Add("@otyp", NpgsqlDbType.Text);
         command.Parameters.Add("@cont", NpgsqlDbType.Text);
         command.Parameters.Add("@typid", NpgsqlDbType.Text);
         command.Prepare();
#endif
         try
         {
            IEnumerable<IIfcSpatialStructureElement> spatialStructure = _model.Instances.OfType<IIfcSpatialStructureElement>();
            foreach (IIfcSpatialStructureElement sse in spatialStructure)
            {
               // do something
               string guid = sse.GlobalId.ToString();
               int IfcLineNo = sse.EntityLabel;

               string elementtype = sse.GetType().Name.ToUpper();
               string typeID = String.Empty;
               int typeLineNo = 0;
               IEnumerable<IIfcRelDefinesByType> relTyp = sse.IsTypedBy;
               if (relTyp != null || relTyp.Count() > 0)
               {
                  IIfcRelDefinesByType typ = relTyp.FirstOrDefault();
                  if (typ != null)
                  {
                     typeID = typ.RelatingType.GlobalId.ToString();
                     typeLineNo = typ.RelatingType.EntityLabel;
                  }
               }
               //if (sse.GetDefiningType() != null)
               //  {
               //      typeID = sse.GetDefiningType().GlobalId;
               //      typeLineNo = sse.GetDefiningType().EntityLabel;
               //  }
               string sseName = BIMRLUtils.checkSingleQuote(sse.Name);
               string sseDescription = BIMRLUtils.checkSingleQuote(sse.Description);
               string sseObjectType = BIMRLUtils.checkSingleQuote(sse.ObjectType);
               string sseLongName = BIMRLUtils.checkSingleQuote(sse.LongName);
               IIfcRelAggregates relContainer = sse.Decomposes.FirstOrDefault();
               if (relContainer == null)
                  container = string.Empty;
               else
                  container = relContainer.RelatingObject.GlobalId.ToString();

               // Keep a mapping between IFC guid used as a key in BIMRL and the IFC line no of the entity
               _refBIMRLCommon.guidLineNoMappingAdd(BIMRLProcessModel.currModelID, IfcLineNo, guid);
#if ORACLE
               sqlStmt = "Insert into " + DBOperation.formatTabName("BIMRL_Element") + "(Elementid, LineNo, ElementType, ModelID, Name, LongName, Description, ObjectType, Container, TypeID) Values ('"
                           + guid + "'," + IfcLineNo + ", '" + elementtype + "', " + BIMRLProcessModel.currModelID.ToString() + ", '" + sseName + "', '" + sseLongName + "','" + sseDescription + "', '" + sseObjectType 
                           + "', '" + container + "', '" + typeID + "')";
               command.CommandText = sqlStmt;
#endif
#if POSTGRES
               command.Parameters["@eid"].Value = guid;
               command.Parameters["@line"].Value = IfcLineNo;
               command.Parameters["@etyp"].Value = elementtype;
               command.Parameters["@modid"].Value = BIMRLProcessModel.currModelID;

               if (string.IsNullOrEmpty(sseName))
                  command.Parameters["@name"].Value = DBNull.Value;
               else
                  command.Parameters["@name"].Value = sseName;

               if (string.IsNullOrEmpty(sseLongName))
                  command.Parameters["@lname"].Value = DBNull.Value;
               else
                  command.Parameters["@lname"].Value = sseLongName;

               if (string.IsNullOrEmpty(sseDescription))
                  command.Parameters["@desc"].Value = DBNull.Value;
               else
                  command.Parameters["@desc"].Value = sseDescription;

               if (string.IsNullOrEmpty(sseObjectType))
                  command.Parameters["@otyp"].Value = DBNull.Value;
               else
                  command.Parameters["@otyp"].Value = sseObjectType;

               if (string.IsNullOrEmpty(container))
                  command.Parameters["@cont"].Value = DBNull.Value;
               else
                  command.Parameters["@cont"].Value = container;

               if (string.IsNullOrEmpty(typeID))
                  command.Parameters["@typid"].Value = DBNull.Value;
               else
                  command.Parameters["@typid"].Value = typeID;
#endif
               currStep = sqlStmt;
               commandStatus = command.ExecuteNonQuery();

               // Add intormation of the product label (LineNo into a List for the use later to update the Geometry 
               _refBIMRLCommon.insEntityLabelListAdd(Math.Abs(IfcLineNo));
               currInsertCount++;

               if (currInsertCount % DBOperation.commitInterval == 0)
               {
                  //Do commit at interval but keep the long transaction (reopen)
                  DBOperation.commitTransaction();
               }
            }
         }
#if ORACLE
         catch (OracleException e)
#endif
#if POSTGRES
         catch (NpgsqlException e)
#endif
         {
            string excStr = "%%Error - " + e.Message + "\n\t" + currStep;
            _refBIMRLCommon.StackPushError(excStr);
            command.Dispose();
            throw;
         }

         DBOperation.commitTransaction();
         command.Dispose();
      }

      private void processSpatialStructureRel()
      {
         string SqlStmt;
         string currStep = string.Empty;

         DBOperation.beginTransaction();

         int commandStatus = -1;
         int currInsertCount = 0;
         string parentGuid = string.Empty;
         string parentType = string.Empty;

#if ORACLE
         OracleCommand command = new OracleCommand(" ", DBOperation.DBConn);
#endif
#if POSTGRES
         NpgsqlCommand command = new NpgsqlCommand(" ", DBOperation.DBConn);
#endif

         try
         {
            IEnumerable<IIfcSpatialStructureElement> spatialStructure = _model.Instances.OfType<IIfcSpatialStructureElement>();
            foreach (IIfcSpatialStructureElement sse in spatialStructure)
            {
               // Insert itself at levelremoved=0
               int levelRemoved = 0;
               SqlStmt = "insert into " + DBOperation.formatTabName("BIMRL_SPATIALSTRUCTURE") + "(SPATIALELEMENTID, SPATIALELEMENTTYPE, PARENTID, PARENTTYPE, LEVELREMOVED)"
                  + " values ('" + sse.GlobalId.ToString() + "','" + sse.GetType().Name.ToUpper() + "','"
                  + sse.GlobalId.ToString() + "','" + sse.GetType().Name.ToUpper() + "'," + levelRemoved + ")";
               command.CommandText = SqlStmt;
               currStep = SqlStmt;
               commandStatus = command.ExecuteNonQuery();

               currInsertCount++;

               levelRemoved = 1;
               // insert aggregation relationship with the parent
               IEnumerable<IIfcRelAggregates> decomposes = sse.Decomposes;
               foreach (IIfcRelAggregates relAgg in decomposes)
               {
                  IIfcSpatialStructureElement parent = relAgg.RelatingObject as IIfcSpatialStructureElement;
                  if (parent == null)
                  {
                     parentGuid = string.Empty;
                     parentType = string.Empty;
                     levelRemoved = 0;
                  }
                  else
                  {
                     parentGuid = parent.GlobalId.ToString();
                     parentType = parent.GetType().Name.ToUpper();
                  }
#if ORACLE
                  SqlStmt = "insert into " + DBOperation.formatTabName("BIMRL_SPATIALSTRUCTURE") + "(SPATIALELEMENTID, SPATIALELEMENTTYPE, PARENTID, PARENTTYPE, LEVELREMOVED)"
                              + " values ('" + sse.GlobalId.ToString() + "','" + sse.GetType().Name.ToUpper() + "','"
                              + parentGuid + "','" + parentType + "'," + levelRemoved + ")";
                  command.CommandText = SqlStmt;
#endif
#if POSTGRES
                  SqlStmt = "insert into " + DBOperation.formatTabName("BIMRL_SPATIALSTRUCTURE") + "(SPATIALELEMENTID, SPATIALELEMENTTYPE, PARENTID, PARENTTYPE, LEVELREMOVED)"
                           + " values (@sid, @setyp, @pid, @ptyp, @lvrm)";
                  command.CommandText = SqlStmt;
                  command.Parameters.Clear();
                  command.Parameters.AddWithValue("@sid", NpgsqlDbType.Text, sse.GlobalId.ToString());
                  command.Parameters.AddWithValue("@setyp", NpgsqlDbType.Text, sse.GetType().Name.ToUpper());

                  if (string.IsNullOrEmpty(parentGuid))
                     command.Parameters.AddWithValue("@pid", NpgsqlDbType.Text, DBNull.Value);
                  else
                     command.Parameters.AddWithValue("@pid", NpgsqlDbType.Text, parentGuid);

                  if (string.IsNullOrEmpty(parentType))
                     command.Parameters.AddWithValue("@ptyp", NpgsqlDbType.Text, DBNull.Value);
                  else
                     command.Parameters.AddWithValue("@ptyp", NpgsqlDbType.Text, parentType);

                  command.Parameters.AddWithValue("@lvrm", NpgsqlDbType.Integer, levelRemoved);

#endif
                  currStep = SqlStmt;
                  commandStatus = command.ExecuteNonQuery();

                  currInsertCount++;

                  while (parent != null)
                  {
                     levelRemoved++;
                     IEnumerable<IIfcRelAggregates> decomposesGP = parent.Decomposes;
                     if (decomposesGP.Count() == 0)
                        break;
                     // The decomposes attribute is a set of [0,1] only
                     IIfcSpatialStructureElement grandparent = decomposesGP.FirstOrDefault().RelatingObject as IIfcSpatialStructureElement;
                     if (grandparent == null)
                        break;  // no ancestor to insert anymore

                     SqlStmt = "insert into " + DBOperation.formatTabName("BIMRL_SPATIALSTRUCTURE") + "(SPATIALELEMENTID, SPATIALELEMENTTYPE, PARENTID, PARENTTYPE, LEVELREMOVED)"
                                 + " values ('" + sse.GlobalId.ToString() + "','" + sse.GetType().Name.ToUpper() + "','"
                                 + grandparent.GlobalId.ToString() + "','" + grandparent.GetType().Name.ToUpper() + "'," + levelRemoved + ")";
                     command.CommandText = SqlStmt;
                     currStep = SqlStmt;
                     commandStatus = command.ExecuteNonQuery();
                     currInsertCount++;
                     parent = grandparent;
                  }

                  if (currInsertCount % DBOperation.commitInterval == 0)
                  {
                     //Do commit at interval but keep the long transaction (reopen)
                     DBOperation.commitTransaction();
                  }
               }
            }
         }
#if ORACLE
         catch (OracleException e)
#endif
#if POSTGRES
         catch (NpgsqlException e)
#endif
         {
            string excStr = "%%Error - " + e.Message + "\n\t" + currStep;
            _refBIMRLCommon.StackPushError(excStr);
            command.Dispose();
            throw;
         }

         DBOperation.commitTransaction();
         command.Dispose();
      }
   }
}
