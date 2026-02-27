import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, IconButton, Alert, CircularProgress, Box, Chip,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Doctor, DoctorList } from '../types/doctor';

export default function DoctorListPage() {
  const navigate = useNavigate();
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDoctors = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const link = await apiClient.getLink('doctors');
      const data = await apiClient.get<DoctorList>(link.href);
      setDoctors(data.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load doctors');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchDoctors(); }, [fetchDoctors]);

  const handleDelete = async (doctor: Doctor) => {
    const deleteLink = apiClient.findLink(doctor.links, 'delete');
    if (!deleteLink) return;

    if (!confirm(`Delete Dr. ${doctor.firstName} ${doctor.lastName}?`)) return;

    try {
      await apiClient.delete(deleteLink.href);
      await fetchDoctors();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete doctor');
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Doctors</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/doctors/new')}>
          Add Doctor
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {doctors.length === 0 ? (
        <Alert severity="info">No doctors found. Add a new doctor to get started.</Alert>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Specialty</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Phone</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {doctors.map((doctor) => (
                <TableRow key={doctor.id} hover>
                  <TableCell>{doctor.firstName} {doctor.lastName}</TableCell>
                  <TableCell>
                    <Chip label={doctor.specialty} size="small" />
                  </TableCell>
                  <TableCell>{doctor.email}</TableCell>
                  <TableCell>{doctor.phone}</TableCell>
                  <TableCell align="right">
                    <IconButton aria-label="view" onClick={() => navigate(`/doctors/${doctor.id}`)}>
                      <VisibilityIcon />
                    </IconButton>
                    <IconButton aria-label="edit" onClick={() => navigate(`/doctors/${doctor.id}/edit`)}>
                      <EditIcon />
                    </IconButton>
                    <IconButton aria-label="delete" onClick={() => handleDelete(doctor)} color="error">
                      <DeleteIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  );
}
