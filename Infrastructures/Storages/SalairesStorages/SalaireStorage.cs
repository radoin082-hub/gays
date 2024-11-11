﻿using GestionPersonnel.Models.Salaires;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionPersonnel.Storages.SalairesStorages
{

    public class SalaireStorage
    {
        private readonly string _connectionString;

        public SalaireStorage(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DBConnection");
        }

        private const string _selectAllQuery = "SELECT * FROM Salaires";
        private const string _selectByIdQuery = "SELECT * FROM Salaires WHERE SalaireID = @id";
        private const string _insertQuery = "INSERT INTO Salaires (EmployeID, Mois, Salaire, Primes, Avances, Dettes, SalaireNet, TypePaiementID) VALUES (@EmployeID, @Mois, @Salaire, @Primes, @Avances, @Dettes, @SalaireNet, @TypePaiementID); SELECT SCOPE_IDENTITY();";
        private const string _updateQuery = "UPDATE Salaires SET EmployeID = @EmployeID, Mois = @Mois, Salaire = @Salaire, Primes = @Primes, Avances = @Avances, Dettes = @Dettes, SalaireNet = @SalaireNet, TypePaiementID = @TypePaiementID WHERE SalaireID = @SalaireID;";
        private const string _deleteQuery = "DELETE FROM Salaires WHERE SalaireID = @SalaireID;";
        private const string _updateSalarireDetteQuery = "UPDATE Salaires SET Dettes = @Dettes WHERE EmployeID = @EmployeID AND Year(Mois)=Year(@Mois) And Month(Mois)=Month(@Mois);";
        private static Salaire GetSalaireFromDataRow(DataRow row)
        {
            return new Salaire
            {
                SalaireID = (int)row["SalaireID"],
                EmployeID = (int)row["EmployeID"],
                Mois = (DateTime)row["Mois"],
                Salairee = (decimal)row["Salaire"],
                Primes = (decimal)row["Primes"],
                Avances = (decimal)row["Avances"],
                Dettes = (decimal)row["Dettes"],
                SalaireNet = (decimal)row["SalaireNet"],
                TypePaiementID = (int)row["TypePaiementID"]
            };
        }

        public async Task<List<Salaire>> GetAll()
        {
            await using var connection = new SqlConnection(_connectionString);
            SqlCommand cmd = new(_selectAllQuery, connection);

            DataTable dataTable = new();
            SqlDataAdapter da = new(cmd);

            connection.Open();
            da.Fill(dataTable);

            return (from DataRow row in dataTable.Rows select GetSalaireFromDataRow(row)).ToList();
        }
        
        public async Task<Salaire?> GetById(int id)
        {
            await using var connection = new SqlConnection(_connectionString);

            SqlCommand cmd = new(_selectByIdQuery, connection);
            cmd.Parameters.AddWithValue("@id", id);

            DataTable dataTable = new();
            SqlDataAdapter da = new(cmd);

            connection.Open();
            da.Fill(dataTable);

            return dataTable.Rows.Count == 0 ? null : GetSalaireFromDataRow(dataTable.Rows[0]);
        }

        public async Task Add(Salaire salaire)
        {
            await using var connection = new SqlConnection(_connectionString);
            SqlCommand cmd = new(_insertQuery, connection);
            cmd.Parameters.AddWithValue("@EmployeID", salaire.EmployeID);
            cmd.Parameters.AddWithValue("@Mois", salaire.Mois);
            cmd.Parameters.AddWithValue("@Salaire", salaire.Salairee);
            cmd.Parameters.AddWithValue("@Primes", salaire.Primes);
            cmd.Parameters.AddWithValue("@Avances", salaire.Avances);
            cmd.Parameters.AddWithValue("@Dettes", salaire.Dettes);
            cmd.Parameters.AddWithValue("@SalaireNet", salaire.SalaireNet);
            cmd.Parameters.AddWithValue("@TypePaiementID", salaire.TypePaiementID);

            connection.Open();
            var id = await cmd.ExecuteScalarAsync();
            salaire.SalaireID = Convert.ToInt32(id);
        }

        public async Task Update(Salaire salaire)
        {
            await using var connection = new SqlConnection(_connectionString);
            SqlCommand cmd = new(_updateQuery, connection);
            cmd.Parameters.AddWithValue("@EmployeID", salaire.EmployeID);
            cmd.Parameters.AddWithValue("@Mois", salaire.Mois);
            cmd.Parameters.AddWithValue("@Salaire", salaire.Salairee);
            cmd.Parameters.AddWithValue("@Primes", salaire.Primes);
            cmd.Parameters.AddWithValue("@Avances", salaire.Avances);
            cmd.Parameters.AddWithValue("@Dettes", salaire.Dettes);
            cmd.Parameters.AddWithValue("@SalaireNet", salaire.SalaireNet);
            cmd.Parameters.AddWithValue("@TypePaiementID", salaire.TypePaiementID);
            cmd.Parameters.AddWithValue("@SalaireID", salaire.SalaireID);

            connection.Open();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Delete(int id)
        {
            await using var connection = new SqlConnection(_connectionString);
            SqlCommand cmd = new(_deleteQuery, connection);
            cmd.Parameters.AddWithValue("@SalaireID", id);

            connection.Open();
            await cmd.ExecuteNonQueryAsync();
        }
        public List<SalaireDetail> GetSalaireDetails()
        {
            List<SalaireDetail> salaireDetailsList = new List<SalaireDetail>();

            string query = @"
            SELECT TOP (1000) 
                s.SalaireID,
                s.Salaire,
                s.Primes,
                s.Avances,
                s.Dettes,
                s.SalaireNet,
                e.Nom AS NomEmploye,
                e.Prenom AS PrenomEmploye,
                f.NomFonction,
                t.NomTypePaiement
            FROM 
                [db_aa9d4f_gestionpersonnel].[dbo].[Salaires] s
            JOIN 
                [db_aa9d4f_gestionpersonnel].[dbo].[Employes] e ON s.EmployeID = e.EmployeID
            JOIN 
                [db_aa9d4f_gestionpersonnel].[dbo].[Fonctions] f ON e.FonctionID = f.FonctionID
            JOIN 
                [db_aa9d4f_gestionpersonnel].[dbo].[TypesDePaiement] t ON s.TypePaiementID = t.TypePaiementID";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        salaireDetailsList.Add(new SalaireDetail
                        {
                            Salaire = reader.IsDBNull(reader.GetOrdinal("Salaire")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Salaire")),
                            Primes = reader.IsDBNull(reader.GetOrdinal("Primes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Primes")),
                            Avances = reader.IsDBNull(reader.GetOrdinal("Avances")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Avances")),
                            Dettes = reader.IsDBNull(reader.GetOrdinal("Dettes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Dettes")),
                            SalaireNet = reader.IsDBNull(reader.GetOrdinal("SalaireNet")) ? 0 : reader.GetDecimal(reader.GetOrdinal("SalaireNet")),
                            NomEmploye = reader.IsDBNull(reader.GetOrdinal("NomEmploye")) ? string.Empty : reader.GetString(reader.GetOrdinal("NomEmploye")),
                            PrenomEmploye = reader.IsDBNull(reader.GetOrdinal("PrenomEmploye")) ? string.Empty : reader.GetString(reader.GetOrdinal("PrenomEmploye")),
                            NomFonction = reader.IsDBNull(reader.GetOrdinal("NomFonction")) ? string.Empty : reader.GetString(reader.GetOrdinal("NomFonction")),
                            TypePaiement = reader.IsDBNull(reader.GetOrdinal("NomTypePaiement")) ? string.Empty : reader.GetString(reader.GetOrdinal("NomTypePaiement"))
                        });

                    }
                }
            }

            return salaireDetailsList;
        }
        public async Task<List<SalaireDetail>> GetSalariesByMonth(DateTime mois)
        {
            List<SalaireDetail> salaires = new List<SalaireDetail>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand("GetSalariesByMonth", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Mois", mois);

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            SalaireDetail salaireDetails = new SalaireDetail
                            {
                                NomEmploye = reader["NomEmploye"].ToString(),
                                PrenomEmploye = reader["PrenomEmploye"].ToString(),
                                NomFonction = reader["NomFonction"].ToString(),
                                Salaire = Convert.ToDecimal(reader["Salaire"]),
                                Primes = Convert.ToDecimal(reader["Primes"]),
                                Avances = Convert.ToDecimal(reader["Avances"]),
                                Dettes = Convert.ToDecimal(reader["Dettes"]),
                                SalaireNet = Convert.ToDecimal(reader["SalaireNet"]),
                                TypePaiement = reader["TypePaiement"].ToString()
                            };

                            salaires.Add(salaireDetails);
                        }
                    }
                }
            }

            return salaires;
        }

        public async Task UpdateDette(int employeeid, Decimal dette,DateTime mois)
        {
            await using var connection = new SqlConnection(_connectionString);
            SqlCommand cmd = new(_updateSalarireDetteQuery, connection);
            cmd.Parameters.AddWithValue("@EmployeID", employeeid);
            cmd.Parameters.AddWithValue("@Dettes", dette);
            cmd.Parameters.AddWithValue("@Mois", mois);
            connection.Open();
            await cmd.ExecuteNonQueryAsync();
        }

    }
}
