import { useState, useEffect } from 'react';
import { Typography, Paper, Box, Button, Alert, CircularProgress, Chip } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam } from '../types/exam';
import type { Patient } from '../types/patient';

export default function ExamDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [exam, setExam] = useState<Exam | null>(null);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchExam = async () => {
      try {
        const data = await apiClient.get<Exam>(`/api/exams/${id}`);
        setExam(data);

        const patientLink = apiClient.findLink(data.links, 'patient');
        if (patientLink) {
          const patientData = await apiClient.get<Patient>(patientLink.href);
          setPatient(patientData);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load exam');
      } finally {
        setLoading(false);
      }
    };
    fetchExam();
  }, [id]);

  const statusColor = (status: string): 'default' | 'primary' | 'success' | 'error' => {
    switch (status.toLowerCase()) {
      case 'scheduled': return 'primary';
      case 'completed': return 'success';
      case 'cancelled': return 'error';
      default: return 'default';
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !exam) {
    return <Alert severity="error">{error ?? 'Exam not found'}</Alert>;
  }

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(`/patients/${exam.patientId}/exams`)}>
          Back to Exams
        </Button>
        <Button variant="contained" startIcon={<EditIcon />} onClick={() => navigate(`/exams/${id}/edit`)}>
          Edit
        </Button>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          {exam.type}
        </Typography>
        {patient && (
          <Typography variant="subtitle1" color="text.secondary" gutterBottom>
            Patient:{' '}
            <Button size="small" onClick={() => navigate(`/patients/${patient.id}`)}>
              {patient.firstName} {patient.lastName}
            </Button>
          </Typography>
        )}
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3, mt: 2 }}>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Scheduled Date</Typography>
            <Typography>{new Date(exam.scheduledDate).toLocaleDateString()}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Status</Typography>
            <Chip label={exam.status} color={statusColor(exam.status)} size="small" />
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Results</Typography>
            <Typography>{exam.results ?? 'No results yet'}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Notes</Typography>
            <Typography>{exam.notes ?? 'No notes'}</Typography>
          </Box>
        </Box>
      </Paper>
    </>
  );
}
