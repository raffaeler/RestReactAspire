import { useState, useEffect } from 'react';
import {
  Typography, Paper, Box, Button, TextField, Alert, CircularProgress,
  MenuItem,
} from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam, CreateExamRequest, UpdateExamRequest } from '../types/exam';
import type { Patient, PatientList } from '../types/patient';
import type { Doctor, DoctorList } from '../types/doctor';
import type { Link } from '../types/hateoas';

const examStatuses = ['Scheduled', 'Completed', 'Cancelled'];

export default function ExamFormPage() {
  const { id, patientId } = useParams<{ id: string; patientId: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(id);

  const [formData, setFormData] = useState<CreateExamRequest>({
    patientId: patientId ?? '',
    doctorId: null,
    type: '',
    scheduledDate: '',
    status: 'Scheduled',
    results: null,
    notes: null,
  });
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [examLinks, setExamLinks] = useState<Link[]>([]);
  const [examPatientId, setExamPatientId] = useState<string | null>(null);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const needsPatientSelector = !patientId && !isEdit;

  useEffect(() => {
    const fetchDoctors = async () => {
      try {
        const link = await apiClient.getLink('doctors');
        const data = await apiClient.get<DoctorList>(link.href);
        setDoctors(data.items);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load doctors');
      }
    };
    fetchDoctors();
  }, []);

  useEffect(() => {
    if (!needsPatientSelector) return;

    const fetchPatients = async () => {
      try {
        const link = await apiClient.getLink('patients');
        const data = await apiClient.get<PatientList>(link.href);
        setPatients(data.items);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load patients');
      }
    };
    fetchPatients();
  }, [needsPatientSelector]);

  useEffect(() => {
    if (!isEdit) return;

    const fetchExam = async () => {
      try {
        const data = await apiClient.get<Exam>(`/api/exams/${id}`);
        setFormData({
          patientId: data.patientId,
          doctorId: data.doctorId,
          type: data.type,
          scheduledDate: data.scheduledDate,
          status: data.status,
          results: data.results,
          notes: data.notes,
        });
        setExamLinks(data.links);
        setExamPatientId(data.patientId);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load exam');
      } finally {
        setLoading(false);
      }
    };
    fetchExam();
  }, [id, isEdit]);

  const handleChange = (field: keyof CreateExamRequest) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    if (field === 'results' || field === 'notes') {
      setFormData(prev => ({ ...prev, [field]: value || null }));
    } else if (field === 'doctorId') {
      setFormData(prev => ({ ...prev, [field]: value || null }));
    } else {
      setFormData(prev => ({ ...prev, [field]: value }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (isEdit) {
        const updateLink = apiClient.findLink(examLinks, 'update');
        if (!updateLink) throw new Error('Update link not available');
        const updateRequest: UpdateExamRequest = {
          doctorId: formData.doctorId,
          type: formData.type,
          scheduledDate: formData.scheduledDate,
          status: formData.status,
          results: formData.results,
          notes: formData.notes,
        };
        await apiClient.put<Exam>(updateLink.href, updateRequest);
        const pid = examPatientId ?? patientId;
        navigate(pid ? `/patients/${pid}/exams` : '/exams');
      } else {
        const link = await apiClient.getLink('exams');
        await apiClient.post<Exam>(link.href, formData);
        navigate(patientId ? `/patients/${patientId}/exams` : '/exams');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save exam');
    } finally {
      setSaving(false);
    }
  };

  const backUrl = isEdit
    ? (examPatientId ? `/patients/${examPatientId}/exams` : '/exams')
    : (patientId ? `/patients/${patientId}/exams` : '/exams');

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(backUrl)}>
          Back to Exams
        </Button>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          {isEdit ? 'Edit Exam' : 'New Exam'}
        </Typography>

        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {needsPatientSelector && (
            <TextField
              label="Patient"
              select
              value={formData.patientId}
              onChange={handleChange('patientId')}
              required
            >
              {patients.map(p => (
                <MenuItem key={p.id} value={p.id}>{p.firstName} {p.lastName}</MenuItem>
              ))}
            </TextField>
          )}
          <TextField
            label="Doctor"
            select
            value={formData.doctorId ?? ''}
            onChange={handleChange('doctorId')}
          >
            <MenuItem value="">
              <em>No doctor assigned</em>
            </MenuItem>
            {doctors.map(d => (
              <MenuItem key={d.id} value={d.id}>Dr. {d.firstName} {d.lastName} — {d.specialty}</MenuItem>
            ))}
          </TextField>
          <TextField label="Type" value={formData.type} onChange={handleChange('type')} required
            placeholder="e.g., Blood Test, X-Ray, MRI" />
          <TextField
            label="Scheduled Date"
            type="date"
            value={formData.scheduledDate}
            onChange={handleChange('scheduledDate')}
            required
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="Status"
            select
            value={formData.status}
            onChange={handleChange('status')}
            required
          >
            {examStatuses.map(s => (
              <MenuItem key={s} value={s}>{s}</MenuItem>
            ))}
          </TextField>
          <TextField label="Results" value={formData.results ?? ''} onChange={handleChange('results')}
            multiline rows={3} placeholder="Enter exam results (optional)" />
          <TextField label="Notes" value={formData.notes ?? ''} onChange={handleChange('notes')}
            multiline rows={2} placeholder="Additional notes (optional)" />

          <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
            <Button variant="outlined" onClick={() => navigate(backUrl)}>
              Cancel
            </Button>
          </Box>
        </Box>
      </Paper>
    </>
  );
}
