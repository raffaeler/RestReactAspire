import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Alert, CircularProgress, Box, Paper, Card,
  CardContent, Stack, Divider,
} from '@mui/material';
import StorageIcon from '@mui/icons-material/Storage';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import BarChartIcon from '@mui/icons-material/BarChart';
import { apiClient } from '../api/apiClient';

interface StatsResponse {
  patientCount: number;
  doctorCount: number;
  examCount: number;
}

interface SeedResponse {
  patientsCreated: number;
  doctorsCreated: number;
  examsCreated: number;
}

interface ResetResponse {
  patientsDeleted: number;
  doctorsDeleted: number;
  examsDeleted: number;
}

export default function AdminPage() {
  const [stats, setStats] = useState<StatsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const fetchStats = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const link = await apiClient.getLink('admin-stats');
      const data = await apiClient.get<StatsResponse>(link.href);
      setStats(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load stats');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchStats(); }, [fetchStats]);

  const handleSeed = async () => {
    setActionLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const link = await apiClient.getLink('admin-seed');
      const data = await apiClient.post<SeedResponse>(link.href, {});
      setSuccess(
        `Database seeded: ${data.patientsCreated} patients, ${data.doctorsCreated} doctors, ${data.examsCreated} exams created.`
      );
      await fetchStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to seed database');
    } finally {
      setActionLoading(false);
    }
  };

  const handleReset = async () => {
    if (!confirm('Are you sure you want to reset the database? All data will be permanently deleted.')) return;

    setActionLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const link = await apiClient.getLink('admin-reset');
      const data = await apiClient.post<ResetResponse>(link.href, {});
      setSuccess(
        `Database reset: ${data.patientsDeleted} patients, ${data.doctorsDeleted} doctors, ${data.examsDeleted} exams deleted.`
      );
      await fetchStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reset database');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Admin</Typography>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert>}

      <Paper sx={{ p: 3, mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
          <BarChartIcon sx={{ mr: 1 }} />
          <Typography variant="h6">Database Statistics</Typography>
        </Box>
        <Divider sx={{ mb: 2 }} />
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
            <CircularProgress size={24} />
          </Box>
        ) : stats ? (
          <Stack direction="row" spacing={3}>
            <Card variant="outlined" sx={{ flex: 1 }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>Patients</Typography>
                <Typography variant="h3">{stats.patientCount}</Typography>
              </CardContent>
            </Card>
            <Card variant="outlined" sx={{ flex: 1 }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>Doctors</Typography>
                <Typography variant="h3">{stats.doctorCount}</Typography>
              </CardContent>
            </Card>
            <Card variant="outlined" sx={{ flex: 1 }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>Exams</Typography>
                <Typography variant="h3">{stats.examCount}</Typography>
              </CardContent>
            </Card>
          </Stack>
        ) : null}
      </Paper>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>Actions</Typography>
        <Divider sx={{ mb: 2 }} />
        <Stack direction="row" spacing={2}>
          <Button
            variant="contained"
            color="primary"
            startIcon={<StorageIcon />}
            onClick={handleSeed}
            disabled={actionLoading}
          >
            {actionLoading ? 'Working...' : 'Seed Database'}
          </Button>
          <Button
            variant="contained"
            color="error"
            startIcon={<DeleteSweepIcon />}
            onClick={handleReset}
            disabled={actionLoading}
          >
            {actionLoading ? 'Working...' : 'Reset Database'}
          </Button>
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          <strong>Seed</strong> adds 20 patients, 10 doctors, and 30 exams with sample data.
          <strong> Reset</strong> removes all data from the database.
        </Typography>
      </Paper>
    </>
  );
}
