import { useState, useEffect } from 'react';
import { Typography, Paper, Box, Button, Alert, CircularProgress, Chip } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import ScienceIcon from '@mui/icons-material/Science';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Doctor } from '../types/doctor';

export default function DoctorDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [doctor, setDoctor] = useState<Doctor | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDoctor = async () => {
      try {
        const data = await apiClient.get<Doctor>(`/api/doctors/${id}`);
        setDoctor(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load doctor');
      } finally {
        setLoading(false);
      }
    };
    fetchDoctor();
  }, [id]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !doctor) {
    return <Alert severity="error">{error ?? 'Doctor not found'}</Alert>;
  }

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/doctors')}>
          Back to Doctors
        </Button>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button variant="outlined" startIcon={<ScienceIcon />} onClick={() => navigate(`/doctors/${id}/exams`)}>
            Exams
          </Button>
          <Button variant="contained" startIcon={<EditIcon />} onClick={() => navigate(`/doctors/${id}/edit`)}>
            Edit
          </Button>
        </Box>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Dr. {doctor.firstName} {doctor.lastName}
        </Typography>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3, mt: 2 }}>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Specialty</Typography>
            <Chip label={doctor.specialty} size="small" />
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Email</Typography>
            <Typography>{doctor.email}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Phone</Typography>
            <Typography>{doctor.phone}</Typography>
          </Box>
        </Box>
      </Paper>
    </>
  );
}
