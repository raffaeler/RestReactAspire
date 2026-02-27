import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, IconButton, Alert, CircularProgress, Box, Chip,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam, ExamList } from '../types/exam';
import type { Doctor } from '../types/doctor';

export default function DoctorExamListPage() {
  const { doctorId } = useParams<{ doctorId: string }>();
  const navigate = useNavigate();
  const [exams, setExams] = useState<Exam[]>([]);
  const [doctor, setDoctor] = useState<Doctor | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchExams = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const doctorData = await apiClient.get<Doctor>(`/api/doctors/${doctorId}`);
      setDoctor(doctorData);
      const examsLink = apiClient.findLink(doctorData.links, 'exams');
      if (!examsLink) throw new Error('Exams link not found on doctor');
      const data = await apiClient.get<ExamList>(examsLink.href);
      setExams(data.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load exams');
    } finally {
      setLoading(false);
    }
  }, [doctorId]);

  useEffect(() => { fetchExams(); }, [fetchExams]);

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

  const title = doctor
    ? `Exams for Dr. ${doctor.firstName} ${doctor.lastName}`
    : 'Doctor Exams';

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">{title}</Typography>
        <Button startIcon={<ArrowBackIcon />} variant="outlined" onClick={() => navigate(`/doctors/${doctorId}`)}>
          Back to Doctor
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {exams.length === 0 ? (
        <Alert severity="info">No exams assigned to this doctor.</Alert>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Type</TableCell>
                <TableCell>Scheduled Date</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Results</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {exams.map((exam) => (
                <TableRow key={exam.id} hover>
                  <TableCell>{exam.type}</TableCell>
                  <TableCell>{new Date(exam.scheduledDate).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Chip label={exam.status} color={statusColor(exam.status)} size="small" />
                  </TableCell>
                  <TableCell>{exam.results ?? '—'}</TableCell>
                  <TableCell align="right">
                    <IconButton aria-label="view" onClick={() => navigate(`/exams/${exam.id}`)}>
                      <VisibilityIcon />
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
