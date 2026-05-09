import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Paper, Box, Button, Alert, CircularProgress, Chip,
  MenuItem, TextField, Dialog, DialogTitle, DialogContent, DialogActions,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PersonIcon from '@mui/icons-material/Person';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam, AssignDoctorRequest } from '../types/exam';
import type { Patient } from '../types/patient';
import type { Doctor, DoctorList } from '../types/doctor';

export default function ExamDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [exam, setExam] = useState<Exam | null>(null);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [doctor, setDoctor] = useState<Doctor | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [assignOpen, setAssignOpen] = useState(false);
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [selectedDoctorId, setSelectedDoctorId] = useState<string>('');
  const [assigning, setAssigning] = useState(false);

  const fetchExam = useCallback(async () => {
    try {
      const data = await apiClient.get<Exam>(`/api/exams/${id}`);
      setExam(data);

      const patientLink = apiClient.findLink(data.links, 'patient');
      if (patientLink) {
        const patientData = await apiClient.get<Patient>(patientLink.href);
        setPatient(patientData);
      }

      const doctorLink = apiClient.findLink(data.links, 'doctor');
      if (doctorLink) {
        const doctorData = await apiClient.get<Doctor>(doctorLink.href);
        setDoctor(doctorData);
      } else {
        setDoctor(null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load exam');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { fetchExam(); }, [fetchExam]);

  const handleOpenAssign = async () => {
    try {
      const link = await apiClient.getLink('doctors');
      const data = await apiClient.get<DoctorList>(link.href);
      setDoctors(data.items);
      setSelectedDoctorId(exam?.doctorId ?? '');
      setAssignOpen(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load doctors');
    }
  };

  const handleAssign = async () => {
    if (!exam) return;
    setAssigning(true);
    try {
      const assignLink = apiClient.findLink(exam.links, 'assign-doctor');
      if (!assignLink) throw new Error('Assign doctor link not available');
      const request: AssignDoctorRequest = {
        doctorId: selectedDoctorId || null,
      };
      await apiClient.put<Exam>(assignLink.href, request);
      setAssignOpen(false);
      setLoading(true);
      await fetchExam();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign doctor');
    } finally {
      setAssigning(false);
    }
  };

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
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button variant="outlined" startIcon={<PersonIcon />} onClick={handleOpenAssign}>
            {doctor ? 'Change Doctor' : 'Assign Doctor'}
          </Button>
          <Button variant="contained" startIcon={<EditIcon />} onClick={() => navigate(`/exams/${id}/edit`)}>
            Edit
          </Button>
        </Box>
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
        {doctor && (
          <Typography variant="subtitle1" color="text.secondary" gutterBottom>
            Doctor:{' '}
            <Button size="small" onClick={() => navigate(`/doctors/${doctor.id}`)}>
              Dr. {doctor.firstName} {doctor.lastName} ({doctor.specialty})
            </Button>
          </Typography>
        )}
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3, mt: 2 }}>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Scheduled Date</Typography>
            <Typography>{new Date(exam.scheduledDate).toLocaleDateString()}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Scheduled Time</Typography>
            <Typography>{exam.scheduledTime ?? 'Not set'}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Duration</Typography>
            <Typography>{exam.durationMinutes != null ? `${exam.durationMinutes} minutes` : 'Not set'}</Typography>
          </Box>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">End Time</Typography>
            <Typography>{exam.endTime ?? 'Not available'}</Typography>
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

      <Dialog open={assignOpen} onClose={() => setAssignOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{doctor ? 'Change Doctor' : 'Assign Doctor'}</DialogTitle>
        <DialogContent>
          <TextField
            label="Doctor"
            select
            fullWidth
            value={selectedDoctorId}
            onChange={(e) => setSelectedDoctorId(e.target.value)}
            sx={{ mt: 1 }}
          >
            <MenuItem value="">
              <em>No doctor assigned</em>
            </MenuItem>
            {doctors.map(d => (
              <MenuItem key={d.id} value={d.id}>
                Dr. {d.firstName} {d.lastName} — {d.specialty}
              </MenuItem>
            ))}
          </TextField>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssignOpen(false)}>Cancel</Button>
          <Button onClick={handleAssign} variant="contained" disabled={assigning}>
            {assigning ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
