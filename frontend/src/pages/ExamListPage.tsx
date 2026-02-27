import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, IconButton, Alert, CircularProgress, Box, Chip,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { useNavigate, useParams } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam, ExamList } from '../types/exam';
import type { Patient } from '../types/patient';

export default function ExamListPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const navigate = useNavigate();
  const [exams, setExams] = useState<Exam[]>([]);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchExams = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      if (patientId) {
        const patientData = await apiClient.get<Patient>(`/api/patients/${patientId}`);
        setPatient(patientData);
        const examsLink = apiClient.findLink(patientData.links, 'exams');
        if (!examsLink) throw new Error('Exams link not found on patient');
        const data = await apiClient.get<ExamList>(examsLink.href);
        setExams(data.items);
      } else {
        const link = await apiClient.getLink('exams');
        const data = await apiClient.get<ExamList>(link.href);
        setExams(data.items);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load exams');
    } finally {
      setLoading(false);
    }
  }, [patientId]);

  useEffect(() => { fetchExams(); }, [fetchExams]);

  const handleDelete = async (exam: Exam) => {
    const deleteLink = apiClient.findLink(exam.links, 'delete');
    if (!deleteLink) return;

    if (!confirm(`Delete ${exam.type} exam?`)) return;

    try {
      await apiClient.delete(deleteLink.href);
      await fetchExams();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete exam');
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

  const title = patient
    ? `Exams for ${patient.firstName} ${patient.lastName}`
    : 'All Exams';

  const newExamUrl = patientId ? `/patients/${patientId}/exams/new` : '/exams/new';

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">{title}</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {patient && (
            <Button variant="outlined" onClick={() => navigate(`/patients/${patientId}`)}>
              Back to Patient
            </Button>
          )}
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate(newExamUrl)}>
            Add Exam
          </Button>
        </Box>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {exams.length === 0 ? (
        <Alert severity="info">No exams found. Add a new exam to get started.</Alert>
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
                    <IconButton aria-label="edit" onClick={() => navigate(`/exams/${exam.id}/edit`)}>
                      <EditIcon />
                    </IconButton>
                    <IconButton aria-label="delete" onClick={() => handleDelete(exam)} color="error">
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
