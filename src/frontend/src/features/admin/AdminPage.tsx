import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, ScrollText, Search, Users } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/layout/states'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect } from '@/components/ui/native-select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useAuthenticatedUser } from '@/hooks/use-current-user'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { formatDateTime } from '@/lib/format'
import { adminApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'

const PAGE_SIZE = 20

const ACTION_FILTERS = [
  { value: '', label: '全部動作' },
  { value: 'auth.login', label: '登入' },
  { value: 'auth.register', label: '註冊' },
  { value: 'auth.password', label: '密碼' },
  { value: 'auth.email', label: 'Email 驗證' },
  { value: 'user.', label: '使用者資料' },
  { value: 'admin.', label: '管理操作' },
]

function Pagination({
  page,
  totalPages,
  onChange,
}: {
  page: number
  totalPages: number
  onChange: (page: number) => void
}) {
  if (totalPages <= 1) return null
  return (
    <nav aria-label="分頁" className="flex items-center justify-end gap-2 pt-4">
      <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        <ChevronLeft aria-hidden="true" />
        上一頁
      </Button>
      <span className="text-sm text-muted-foreground" aria-live="polite">
        第 {page} / {totalPages} 頁
      </span>
      <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
        下一頁
        <ChevronRight aria-hidden="true" />
      </Button>
    </nav>
  )
}

function SummaryCards() {
  const summary = useQuery({ queryKey: queryKeys.adminSummary, queryFn: ({ signal }) => adminApi.summary(signal) })

  if (summary.isPending) return <LoadingState />
  if (summary.isError) return <ErrorState onRetry={() => void summary.refetch()} />

  const items = [
    { label: '使用者總數', value: summary.data.totalUsers },
    { label: '已驗證 Email', value: summary.data.confirmedUsers },
    { label: '近 7 天新註冊', value: summary.data.newUsersLast7Days },
    { label: '管理者', value: summary.data.adminCount },
    { label: '24 小時成功登入', value: summary.data.successfulLoginsLast24Hours },
    { label: '24 小時登入失敗', value: summary.data.failedLoginsLast24Hours },
  ]

  return (
    <dl className="grid grid-cols-2 gap-3 lg:grid-cols-3">
      {items.map((item) => (
        <Card key={item.label} className="gap-1 py-4">
          <CardContent className="px-4">
            <dt className="text-sm text-muted-foreground">{item.label}</dt>
            <dd className="mt-1 text-2xl font-bold">{item.value.toLocaleString('zh-TW')}</dd>
          </CardContent>
        </Card>
      ))}
    </dl>
  )
}

function UsersTab({ timeZone }: { timeZone: string }) {
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const params = { page, pageSize: PAGE_SIZE, search: search || undefined }
  const users = useQuery({
    queryKey: queryKeys.adminUsers(params),
    queryFn: ({ signal }) => adminApi.users(params, signal),
    placeholderData: keepPreviousData,
  })

  const onSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>使用者</CardTitle>
        <CardDescription>可依 Email 或顯示名稱搜尋。</CardDescription>
      </CardHeader>
      <CardContent>
        <form role="search" onSubmit={onSearch} className="mb-4 flex gap-2">
          <Label htmlFor="user-search" className="sr-only">
            搜尋使用者
          </Label>
          <Input
            id="user-search"
            placeholder="搜尋 Email 或名稱"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
          <Button type="submit" variant="secondary">
            <Search aria-hidden="true" />
            搜尋
          </Button>
        </form>

        {users.isPending ? (
          <LoadingState />
        ) : users.isError ? (
          <ErrorState onRetry={() => void users.refetch()} />
        ) : users.data.items.length === 0 ? (
          <EmptyState title="找不到使用者" icon={<Users className="size-6" aria-hidden="true" />} />
        ) : (
          <>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>名稱 / Email</TableHead>
                  <TableHead>角色</TableHead>
                  <TableHead>狀態</TableHead>
                  <TableHead>註冊時間</TableHead>
                  <TableHead>最後登入</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.data.items.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>
                      <div className="font-medium">{user.displayName}</div>
                      <div className="text-xs text-muted-foreground">{user.email}</div>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        {user.roles.map((role) => (
                          <Badge key={role} variant={role === 'Admin' ? 'default' : 'secondary'}>
                            {role}
                          </Badge>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {user.emailConfirmed ? (
                          <Badge variant="success">已驗證</Badge>
                        ) : (
                          <Badge variant="outline">未驗證</Badge>
                        )}
                        {user.isLockedOut ? <Badge variant="destructive">鎖定中</Badge> : null}
                      </div>
                    </TableCell>
                    <TableCell className="whitespace-nowrap">{formatDateTime(user.createdAtUtc, timeZone)}</TableCell>
                    <TableCell className="whitespace-nowrap">{formatDateTime(user.lastLoginAtUtc, timeZone)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <Pagination page={users.data.page} totalPages={users.data.totalPages} onChange={setPage} />
          </>
        )}
      </CardContent>
    </Card>
  )
}

function AuditLogsTab({ timeZone }: { timeZone: string }) {
  const [page, setPage] = useState(1)
  const [action, setAction] = useState('')
  const params = { page, pageSize: PAGE_SIZE, action: action || undefined }
  const logs = useQuery({
    queryKey: queryKeys.adminAuditLogs(params),
    queryFn: ({ signal }) => adminApi.auditLogs(params, signal),
    placeholderData: keepPreviousData,
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>稽核紀錄</CardTitle>
        <CardDescription>登入、密碼與管理操作等安全事件。紀錄不包含密碼或 Token。</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="mb-4 max-w-xs">
          <Label htmlFor="audit-action" className="mb-2">
            動作類型
          </Label>
          <NativeSelect
            id="audit-action"
            value={action}
            onChange={(event) => {
              setAction(event.target.value)
              setPage(1)
            }}
          >
            {ACTION_FILTERS.map((filter) => (
              <option key={filter.value} value={filter.value}>
                {filter.label}
              </option>
            ))}
          </NativeSelect>
        </div>

        {logs.isPending ? (
          <LoadingState />
        ) : logs.isError ? (
          <ErrorState onRetry={() => void logs.refetch()} />
        ) : logs.data.items.length === 0 ? (
          <EmptyState title="沒有符合的紀錄" icon={<ScrollText className="size-6" aria-hidden="true" />} />
        ) : (
          <>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>時間</TableHead>
                  <TableHead>動作</TableHead>
                  <TableHead>使用者</TableHead>
                  <TableHead>結果</TableHead>
                  <TableHead>IP</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {logs.data.items.map((log) => (
                  <TableRow key={log.id}>
                    <TableCell className="whitespace-nowrap">{formatDateTime(log.occurredAtUtc, timeZone)}</TableCell>
                    <TableCell>
                      <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{log.action}</code>
                    </TableCell>
                    <TableCell className="max-w-48 truncate">{log.actorEmail ?? '—'}</TableCell>
                    <TableCell>
                      {log.succeeded ? <Badge variant="success">成功</Badge> : <Badge variant="destructive">失敗</Badge>}
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">{log.ipAddress ?? '—'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <Pagination page={logs.data.page} totalPages={logs.data.totalPages} onChange={setPage} />
          </>
        )}
      </CardContent>
    </Card>
  )
}

export function AdminPage() {
  useDocumentTitle('管理後台')
  const user = useAuthenticatedUser()

  return (
    <div>
      <PageHeader title="管理後台" description="使用者與安全事件概覽。題庫與試卷管理將於後續版本加入。" />
      <Tabs defaultValue="overview">
        <TabsList aria-label="管理後台分頁">
          <TabsTrigger value="overview">概覽</TabsTrigger>
          <TabsTrigger value="users">使用者</TabsTrigger>
          <TabsTrigger value="audit">稽核紀錄</TabsTrigger>
        </TabsList>
        <TabsContent value="overview">
          <SummaryCards />
        </TabsContent>
        <TabsContent value="users">
          <UsersTab timeZone={user.timeZoneId} />
        </TabsContent>
        <TabsContent value="audit">
          <AuditLogsTab timeZone={user.timeZoneId} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
