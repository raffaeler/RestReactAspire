import { useState, useEffect } from 'react';
import { Typography, Paper, Box, Button, Alert, CircularProgress } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Patient } from '../types/patient';

export default function PatientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [patient, setPatient] = useState<Patient | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchPatient = async () => {
      try {
        const data = await apiClient.get<Patient>(`/api/patients/${id}`);
        setPatient(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load patient');
      } finally {
        setLoading(false);
      }
    };
    fetchPatient();
  }, [id]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !patient) {
    return <Alert severity="error">{error ?? 'Patient not found'}</Alert>;
  }

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/patients')}>
          Back to Patients
        </Button>
        <Button variant="contained" startIcon={<EditIcon />} onClick={() => navigate(`/patients/${id}/edit`)}>
          Edit
        </Button>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          {patient.firstName} {patient.lastName}
        </Typography>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3, mt: 2 }}>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Date of Birth</Typography>
            <Typography>{new Date(patient.dateOfBirth).toLocaleDateString()}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Email</Typography>
            <Typography>{patient.email}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Phone</Typography>
            <Typography>{patient.phone}</Typography>
          </Box>
        </Box>
      </Paper>
    </>
  );
}
