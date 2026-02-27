import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Alert, CircularProgress, Box, Paper, Grid,
} from '@mui/material';
import {
  PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  LineChart, Line,
} from 'recharts';
import { apiClient } from '../api/apiClient';
import type {
  PatientsByAgeGroupResponse,
  ExamsPerDoctorResponse,
  ExamsOverTimeResponse,
  AvgDurationByExamTypeResponse,
} from '../types/statistics';

const PIE_COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8', '#82ca9d', '#ffc658', '#d0ed57'];
const LINE_COLORS = ['#8884d8', '#82ca9d', '#ffc658', '#ff7300', '#0088FE', '#00C49F', '#FFBB28', '#FF8042',
  '#d0ed57', '#a4de6c', '#d88884', '#84d8d8', '#c49f00', '#8042ff', '#42ff80'];

export default function StatisticsPage() {
  const [ageGroupData, setAgeGroupData] = useState<PatientsByAgeGroupResponse | null>(null);
  const [examsPerDoctorData, setExamsPerDoctorData] = useState<ExamsPerDoctorResponse | null>(null);
  const [examsOverTimeData, setExamsOverTimeData] = useState<ExamsOverTimeResponse | null>(null);
  const [avgDurationData, setAvgDurationData] = useState<AvgDurationByExamTypeResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchAllStatistics = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [ageGroupLink, examsPerDoctorLink, examsOverTimeLink, avgDurationLink] = await Promise.all([
        apiClient.getLink('statistics-patients-by-age-group'),
        apiClient.getLink('statistics-exams-per-doctor'),
        apiClient.getLink('statistics-exams-over-time'),
        apiClient.getLink('statistics-avg-duration-by-exam-type'),
      ]);

      const [ageGroup, examsPerDoctor, examsOverTime, avgDuration] = await Promise.all([
        apiClient.get<PatientsByAgeGroupResponse>(ageGroupLink.href),
        apiClient.get<ExamsPerDoctorResponse>(examsPerDoctorLink.href),
        apiClient.get<ExamsOverTimeResponse>(examsOverTimeLink.href),
        apiClient.get<AvgDurationByExamTypeResponse>(avgDurationLink.href),
      ]);

      setAgeGroupData(ageGroup);
      setExamsPerDoctorData(examsPerDoctor);
      setExamsOverTimeData(examsOverTime);
      setAvgDurationData(avgDuration);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load statistics');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchAllStatistics(); }, [fetchAllStatistics]);

  // Transform avg duration data for the line chart: pivot to { month, type1: val, type2: val, ... }
  const avgDurationLineData = (() => {
    if (!avgDurationData) return { data: [] as Record<string, string | number>[], examTypes: [] as string[] };
    const examTypes = [...new Set(avgDurationData.items.map(i => i.examType))].sort();
    const byMonth = new Map<string, Record<string, string | number>>();
    for (const item of avgDurationData.items) {
      if (!byMonth.has(item.month)) {
        byMonth.set(item.month, { month: item.month });
      }
      byMonth.get(item.month)![item.examType] = item.avgDurationMinutes;
    }
    const data = [...byMonth.values()].sort((a, b) => (a.month as string).localeCompare(b.month as string));
    return { data, examTypes };
  })();

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>;
  }

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Statistics</Typography>
      </Box>

      <Grid container spacing={3}>
        {/* Pie Chart: Patients by Age Group */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Patients by Age Group</Typography>
            {ageGroupData && ageGroupData.items.length > 0 ? (
              <ResponsiveContainer width="100%" height={350}>
                <PieChart>
                  <Pie
                    data={ageGroupData.items}
                    dataKey="count"
                    nameKey="ageGroup"
                    cx="50%"
                    cy="50%"
                    outerRadius={120}
                    label={({ ageGroup, count }) => `${ageGroup}: ${count}`}
                  >
                    {ageGroupData.items.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <Typography color="text.secondary">No data available</Typography>
            )}
          </Paper>
        </Grid>

        {/* Bar Chart: Exams per Doctor */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Exams per Doctor</Typography>
            {examsPerDoctorData && examsPerDoctorData.items.length > 0 ? (
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={examsPerDoctorData.items} margin={{ left: 10, right: 10, bottom: 80 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="doctorName"
                    interval={0}
                    height={100}
                    tick={({ x, y, payload }: { x: number; y: number; payload: { value: string } }) => (
                      <text x={x} y={y + 8} textAnchor="end" fontSize={10} transform={`rotate(-45, ${x}, ${y + 8})`}>
                        {payload.value}
                      </text>
                    )}
                  />
                  <YAxis allowDecimals={false} />
                  <Tooltip
                    formatter={(value: number, _name: string, props: { payload: { specialty: string } }) =>
                      [`${value} exams`, props.payload.specialty]}
                  />
                  <Bar dataKey="examCount" fill="#1976d2" name="Exams" />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <Typography color="text.secondary">No data available</Typography>
            )}
          </Paper>
        </Grid>

        {/* Line Chart: Exams Over Time */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Exams Over Time</Typography>
            {examsOverTimeData && examsOverTimeData.items.length > 0 ? (
              <ResponsiveContainer width="100%" height={350}>
                <LineChart data={examsOverTimeData.items} margin={{ left: 10, right: 10, bottom: 40 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="month"
                    angle={-45}
                    textAnchor="end"
                    tick={{ fontSize: 11 }}
                    height={70}
                  />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="examCount"
                    stroke="#1976d2"
                    name="Exams"
                    strokeWidth={2}
                    dot={{ r: 3 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <Typography color="text.secondary">No data available</Typography>
            )}
          </Paper>
        </Grid>

        {/* Line Chart: Avg Duration by Exam Type Over Time */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Avg Duration by Exam Type Over Time</Typography>
            {avgDurationLineData.data.length > 0 ? (
              <ResponsiveContainer width="100%" height={350}>
                <LineChart data={avgDurationLineData.data} margin={{ left: 10, right: 10, bottom: 40 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="month"
                    angle={-45}
                    textAnchor="end"
                    tick={{ fontSize: 11 }}
                    height={70}
                  />
                  <YAxis unit=" min" />
                  <Tooltip />
                  <Legend wrapperStyle={{ fontSize: 11 }} />
                  {avgDurationLineData.examTypes.map((examType, index) => (
                    <Line
                      key={examType}
                      type="monotone"
                      dataKey={examType}
                      stroke={LINE_COLORS[index % LINE_COLORS.length]}
                      name={examType}
                      strokeWidth={2}
                      dot={{ r: 2 }}
                      connectNulls
                    />
                  ))}
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <Typography color="text.secondary">No data available</Typography>
            )}
          </Paper>
        </Grid>
      </Grid>
    </>
  );
}
